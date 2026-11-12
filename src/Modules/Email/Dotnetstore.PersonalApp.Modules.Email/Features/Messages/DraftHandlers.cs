using Dotnetstore.PersonalApp.Modules.Email.Domain;
using Dotnetstore.PersonalApp.Modules.Email.Features.EmailAccounts;
using Dotnetstore.PersonalApp.Modules.Email.Mail;
using Dotnetstore.PersonalApp.Modules.Email.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using MimeKit;
using Dotnetstore.PersonalApp.Shared.Telemetry;

namespace Dotnetstore.PersonalApp.Modules.Email.Features.Messages;

/// <summary>
/// Saves a message that is not ready to send: a new draft, or (with <c>DraftId</c>) a replacement of one of the user's own drafts,
/// attachments included. Nothing needs to be valid enough to send, but an entirely empty draft is refused.
/// </summary>
internal static class SaveDraft
{
    public sealed record Command(
        Guid UserId,
        Guid? DraftId,
        Guid? AccountId,
        string To,
        string Cc,
        string Subject,
        string Body,
        bool IsHtml,
        IReadOnlyList<AttachmentInput>? Attachments);

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(c => c.UserId).UserIdRules();
            RuleFor(c => c.To).NotNull().MaximumLength(MessageRules.MaxRecipientFieldLength)
                .WithErrorCode("email_recipient_invalid").WithMessage("One of the recipient addresses is not valid.");
            RuleFor(c => c.Cc).NotNull().MaximumLength(MessageRules.MaxRecipientFieldLength)
                .WithErrorCode("email_recipient_invalid").WithMessage("One of the recipient addresses is not valid.");
            RuleFor(c => c.Subject).NotNull().MaximumLength(MessageRules.MaxSubjectLength)
                .WithErrorCode("email_subject_too_long").WithMessage("Subject is too long.");
            RuleFor(c => c.Body).NotNull().MaximumLength(MessageRules.MaxBodyLength)
                .WithErrorCode("email_body_too_long").WithMessage("The message is too long.");
            RuleFor(c => c.Attachments)
                .Must(a => a is null || a.Count <= MailLimits.MaxAttachments)
                .WithErrorCode("email_too_many_attachments").WithMessage("Too many attachments.")
                .Must(a => a is null || a.All(x => x.Content.Length <= MailLimits.MaxAttachmentBytes))
                .WithErrorCode("email_attachment_too_large").WithMessage("An attachment is too large.")
                .Must(a => a is null || a.Sum(x => (long)x.Content.Length) <= MailLimits.MaxTotalAttachmentBytes)
                .WithErrorCode("email_attachments_too_large").WithMessage("The attachments are too large together.");
        }
    }

    public sealed class Handler(IDbContextFactory<EmailDbContext> dbFactory, IValidator<Command> validator, TimeProvider clock)
    {
        public Task<Result<DraftDto>> HandleAsync(Command command, CancellationToken cancellationToken) =>
            AppTelemetry.TraceAsync(this, () => HandleCoreAsync(command, cancellationToken), command);

        private async Task<Result<DraftDto>> HandleCoreAsync(Command command, CancellationToken cancellationToken)
        {
            var validation = await validator.ValidateAsync(command, cancellationToken).ConfigureAwait(false);
            if (!validation.IsValid)
            {
                return validation.ToError();
            }

            var attachments = command.Attachments ?? [];
            if (string.IsNullOrWhiteSpace(command.To) && string.IsNullOrWhiteSpace(command.Cc) && string.IsNullOrWhiteSpace(command.Subject)
                && string.IsNullOrWhiteSpace(command.Body) && attachments.Count == 0)
            {
                return Error.Validation("email_draft_empty", "There is nothing to save yet.");
            }

            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            // Replacing the attachments and writing the new ones must succeed or fail together.
            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

            Draft? draft = null;
            if (command.DraftId is { } id)
            {
                draft = await db.Drafts.FirstOrDefaultAsync(d => d.Id == id && d.UserId == command.UserId, cancellationToken).ConfigureAwait(false);
                if (draft is null)
                {
                    return Error.NotFound("email_draft_not_found", "Draft not found.");
                }

                await db.Attachments.Where(a => a.DraftId == id).ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
            }
            else
            {
                draft = new Draft { Id = Guid.CreateVersion7(), UserId = command.UserId };
                db.Drafts.Add(draft);
            }

            draft.AccountId = command.AccountId;
            draft.To = command.To.Trim();
            draft.Cc = command.Cc.Trim();
            draft.Subject = command.Subject;
            draft.Body = command.Body;
            draft.IsHtml = command.IsHtml;
            draft.UpdatedAt = clock.GetUtcNow();

            foreach (var attachment in attachments)
            {
                db.Attachments.Add(new MailAttachment
                {
                    Id = Guid.CreateVersion7(),
                    DraftId = draft.Id,
                    FileName = attachment.FileName.Length > 255 ? attachment.FileName[..255] : attachment.FileName,
                    ContentType = MimeTypes.GetMimeType(attachment.FileName),
                    Content = attachment.Content,
                });
            }

            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return DraftDto.Summary(draft);
        }
    }
}

/// <summary>Lists the user's own drafts, most recently saved first, without bodies. Drafts are never shared, not even through system accounts.</summary>
internal static class ListDrafts
{
    public const int MaxPageSize = 50;

    public sealed record Query(Guid UserId, int Page = 1, int PageSize = MaxPageSize);

    public sealed record Response(IReadOnlyList<DraftDto> Items, int TotalCount);

    public sealed class Handler(IDbContextFactory<EmailDbContext> dbFactory)
    {
        public Task<Response> HandleAsync(Query query, CancellationToken cancellationToken) =>
            AppTelemetry.TraceAsync(this, () => HandleCoreAsync(query, cancellationToken), query);

        private async Task<Response> HandleCoreAsync(Query query, CancellationToken cancellationToken)
        {
            var page = Math.Max(query.Page, 1);
            var pageSize = Math.Clamp(query.PageSize, 1, MaxPageSize);

            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            var drafts = db.Drafts.AsNoTracking().Where(d => d.UserId == query.UserId);

            var total = await drafts.CountAsync(cancellationToken).ConfigureAwait(false);
            var rows = await drafts
                .OrderByDescending(d => d.UpdatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            return new Response(rows.ConvertAll(DraftDto.Summary), total);
        }
    }
}

internal static class GetDraft
{
    public sealed record Query(Guid Id, Guid UserId);

    public sealed class Handler(IDbContextFactory<EmailDbContext> dbFactory)
    {
        public Task<Result<DraftDto>> HandleAsync(Query query, CancellationToken cancellationToken) =>
            AppTelemetry.TraceAsync(this, () => HandleCoreAsync(query, cancellationToken), query);

        private async Task<Result<DraftDto>> HandleCoreAsync(Query query, CancellationToken cancellationToken)
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            var draft = await db.Drafts.AsNoTracking()
                .FirstOrDefaultAsync(d => d.Id == query.Id && d.UserId == query.UserId, cancellationToken)
                .ConfigureAwait(false);
            if (draft is null)
            {
                return Error.NotFound("email_draft_not_found", "Draft not found.");
            }

            var attachments = await AttachmentQueries.OfDraftAsync(db, draft.Id, cancellationToken).ConfigureAwait(false);
            return DraftDto.Full(draft, attachments);
        }
    }
}

internal static class DeleteDraft
{
    public sealed record Command(Guid Id, Guid UserId);

    public sealed class Handler(IDbContextFactory<EmailDbContext> dbFactory)
    {
        public Task<Result<Guid>> HandleAsync(Command command, CancellationToken cancellationToken) =>
            AppTelemetry.TraceAsync(this, () => HandleCoreAsync(command, cancellationToken), command);

        private async Task<Result<Guid>> HandleCoreAsync(Command command, CancellationToken cancellationToken)
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            var deleted = await db.Drafts
                .Where(d => d.Id == command.Id && d.UserId == command.UserId)
                .ExecuteDeleteAsync(cancellationToken)
                .ConfigureAwait(false);

            return deleted == 0 ? Error.NotFound("email_draft_not_found", "Draft not found.") : command.Id;
        }
    }
}

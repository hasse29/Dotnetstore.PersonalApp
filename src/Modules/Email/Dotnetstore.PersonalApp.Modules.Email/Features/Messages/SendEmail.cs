using Dotnetstore.PersonalApp.Modules.Email.Domain;
using Dotnetstore.PersonalApp.Modules.Email.Features.EmailAccounts;
using Dotnetstore.PersonalApp.Modules.Email.Mail;
using Dotnetstore.PersonalApp.Modules.Email.Persistence;
using Dotnetstore.PersonalApp.Modules.Email.Security;
using Dotnetstore.PersonalApp.Shared.Results;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using MimeKit;

namespace Dotnetstore.PersonalApp.Modules.Email.Features.Messages;

/// <summary>
/// Sends a message through one of the user's SMTP accounts (or a system one), as plain text or HTML, with attachments.
/// The sender is the account's address. A copy is kept in the account's sent mail once the server has accepted it.
/// </summary>
internal static class SendEmail
{
    public sealed record Command(
        Guid UserId,
        Guid AccountId,
        string To,
        string Subject,
        string Body,
        bool IsHtml = false,
        IReadOnlyList<AttachmentInput>? Attachments = null,
        string Cc = "");

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(c => c.UserId).UserIdRules();
            RuleFor(c => c.AccountId).NotEmpty().WithErrorCode("email_account_not_found").WithMessage("Account not found.");
            RuleFor(c => c.To).NotEmpty().WithErrorCode("email_recipient_required").WithMessage("Enter at least one recipient.");
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

    public sealed class Handler(
        IDbContextFactory<EmailDbContext> dbFactory,
        IValidator<Command> validator,
        ISecretProtector protector,
        IMailGateway gateway,
        TimeProvider clock)
    {
        /// <summary>Returns the number of recipients the message was sent to.</summary>
        public async Task<Result<int>> HandleAsync(Command command, CancellationToken cancellationToken)
        {
            var validation = await validator.ValidateAsync(command, cancellationToken).ConfigureAwait(false);
            if (!validation.IsValid)
            {
                return validation.ToError();
            }

            var recipients = MessageRules.ParseRecipients(command.To);
            if (recipients.Count == 0)
            {
                return Error.Validation("email_recipient_required", "Enter at least one recipient.");
            }

            // Someone listed in both fields only gets the message once, as a To recipient.
            var cc = MessageRules.ParseRecipients(command.Cc).Except(recipients, StringComparer.OrdinalIgnoreCase).ToList();
            if (recipients.Count + cc.Count > MessageRules.MaxRecipients)
            {
                return Error.Validation("email_too_many_recipients", "Too many recipients.");
            }

            if (recipients.Concat(cc).Any(r => !EmailAccountRules.IsValidAddress(r)))
            {
                return Error.Validation("email_recipient_invalid", "One of the recipient addresses is not valid.");
            }

            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            var found = await EmailAccountAccess.FindAsync(db, command.AccountId, command.UserId, cancellationToken).ConfigureAwait(false);
            if (!found.IsSuccess)
            {
                return found.Error;
            }

            var account = found.Value;
            if (account.Protocol != MailProtocol.Smtp)
            {
                return Error.Validation("email_account_not_smtp", "Only SMTP accounts can send mail.");
            }

            var server = EmailAccountAccess.ToServer(account, protector);
            if (!server.IsSuccess)
            {
                return server.Error;
            }

            var attachments = (command.Attachments ?? [])
                .Select(a => new MailAttachmentData(a.FileName, MimeTypes.GetMimeType(a.FileName), a.Content))
                .ToList();

            try
            {
                await gateway.SendAsync(
                    server.Value,
                    new OutgoingMessage(account.EmailAddress, recipients, command.Subject, command.Body, command.IsHtml, attachments, cc),
                    cancellationToken).ConfigureAwait(false);
            }
            catch (MailGatewayException ex)
            {
                return Error.Validation(ex.Code, ex.Message);
            }

            var copy = new SentMessage
            {
                Id = Guid.CreateVersion7(),
                AccountId = account.Id,
                To = string.Join(", ", recipients),
                Cc = string.Join(", ", cc),
                Subject = command.Subject,
                Body = command.Body,
                IsHtml = command.IsHtml,
                SentAt = clock.GetUtcNow(),
            };
            db.SentMessages.Add(copy);
            foreach (var attachment in attachments)
            {
                db.Attachments.Add(new MailAttachment
                {
                    Id = Guid.CreateVersion7(),
                    SentMessageId = copy.Id,
                    FileName = Truncate(attachment.FileName, 255),
                    ContentType = Truncate(attachment.ContentType, 200),
                    Content = attachment.Content,
                });
            }

            // The mail is already out: failing to keep a copy must not look like a failed send, or a retry would send it twice.
            try
            {
                await db.SaveChangesAsync(CancellationToken.None).ConfigureAwait(false);
            }
            catch (DbUpdateException)
            {
                // Deliberately ignored, see above.
            }

            return recipients.Count + cc.Count;
        }

        private static string Truncate(string value, int maxLength) => value.Length > maxLength ? value[..maxLength] : value;
    }
}

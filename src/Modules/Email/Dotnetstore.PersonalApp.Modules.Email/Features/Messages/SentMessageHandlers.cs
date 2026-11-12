using Dotnetstore.PersonalApp.Modules.Email.Features.EmailAccounts;
using Dotnetstore.PersonalApp.Modules.Email.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using Microsoft.EntityFrameworkCore;
using Dotnetstore.PersonalApp.Shared.Telemetry;

namespace Dotnetstore.PersonalApp.Modules.Email.Features.Messages;

/// <summary>Lists the copies of sent messages of one account the user may use, newest first, without bodies.</summary>
internal static class ListSentMessages
{
    public const int MaxPageSize = 50;

    public sealed record Query(Guid UserId, Guid AccountId, int Page = 1, int PageSize = MaxPageSize);

    public sealed record Response(IReadOnlyList<SentMessageDto> Items, int TotalCount);

    public sealed class Handler(IDbContextFactory<EmailDbContext> dbFactory)
    {
        public Task<Result<Response>> HandleAsync(Query query, CancellationToken cancellationToken) =>
            AppTelemetry.TraceAsync(this, () => HandleCoreAsync(query, cancellationToken), query);

        private async Task<Result<Response>> HandleCoreAsync(Query query, CancellationToken cancellationToken)
        {
            var page = Math.Max(query.Page, 1);
            var pageSize = Math.Clamp(query.PageSize, 1, MaxPageSize);

            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            var found = await EmailAccountAccess.FindAsync(db, query.AccountId, query.UserId, cancellationToken).ConfigureAwait(false);
            if (!found.IsSuccess)
            {
                return found.Error;
            }

            var messages = db.SentMessages.AsNoTracking().Where(m => m.AccountId == query.AccountId);

            var total = await messages.CountAsync(cancellationToken).ConfigureAwait(false);
            var rows = await messages
                .OrderByDescending(m => m.SentAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            return new Response(rows.ConvertAll(SentMessageDto.Summary), total);
        }
    }
}

internal static class GetSentMessage
{
    public sealed record Query(Guid Id, Guid UserId);

    public sealed class Handler(IDbContextFactory<EmailDbContext> dbFactory)
    {
        public Task<Result<SentMessageDto>> HandleAsync(Query query, CancellationToken cancellationToken) =>
            AppTelemetry.TraceAsync(this, () => HandleCoreAsync(query, cancellationToken), query);

        private async Task<Result<SentMessageDto>> HandleCoreAsync(Query query, CancellationToken cancellationToken)
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            var message = await db.SentMessages.AsNoTracking().FirstOrDefaultAsync(m => m.Id == query.Id, cancellationToken).ConfigureAwait(false);
            var account = message is null
                ? null
                : await EmailAccountAccess.FindAsync(db, message.AccountId, query.UserId, cancellationToken).ConfigureAwait(false);
            if (message is null || account is null || !account.IsSuccess)
            {
                return Error.NotFound("email_message_not_found", "Message not found.");
            }

            var attachments = await AttachmentQueries.OfSentAsync(db, message.Id, cancellationToken).ConfigureAwait(false);
            return SentMessageDto.Full(message, attachments);
        }
    }
}

internal static class DeleteSentMessage
{
    public sealed record Command(Guid Id, Guid UserId);

    public sealed class Handler(IDbContextFactory<EmailDbContext> dbFactory)
    {
        public Task<Result<Guid>> HandleAsync(Command command, CancellationToken cancellationToken) =>
            AppTelemetry.TraceAsync(this, () => HandleCoreAsync(command, cancellationToken), command);

        private async Task<Result<Guid>> HandleCoreAsync(Command command, CancellationToken cancellationToken)
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            var message = await db.SentMessages.FirstOrDefaultAsync(m => m.Id == command.Id, cancellationToken).ConfigureAwait(false);
            var account = message is null
                ? null
                : await EmailAccountAccess.FindAsync(db, message.AccountId, command.UserId, cancellationToken).ConfigureAwait(false);
            if (message is null || account is null || !account.IsSuccess)
            {
                return Error.NotFound("email_message_not_found", "Message not found.");
            }

            db.SentMessages.Remove(message);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return command.Id;
        }
    }
}

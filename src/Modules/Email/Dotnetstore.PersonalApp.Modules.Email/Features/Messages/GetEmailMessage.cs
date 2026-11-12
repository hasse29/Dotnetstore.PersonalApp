using Dotnetstore.PersonalApp.Modules.Email.Features.EmailAccounts;
using Dotnetstore.PersonalApp.Modules.Email.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using Microsoft.EntityFrameworkCore;
using Dotnetstore.PersonalApp.Shared.Telemetry;

namespace Dotnetstore.PersonalApp.Modules.Email.Features.Messages;

internal static class GetEmailMessage
{
    public sealed record Query(Guid Id, Guid UserId);

    public sealed class Handler(IDbContextFactory<EmailDbContext> dbFactory)
    {
        public Task<Result<EmailMessageDto>> HandleAsync(Query query, CancellationToken cancellationToken) =>
            AppTelemetry.TraceAsync(this, () => HandleCoreAsync(query, cancellationToken), query);

        private async Task<Result<EmailMessageDto>> HandleCoreAsync(Query query, CancellationToken cancellationToken)
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            var message = await db.Messages.AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == query.Id, cancellationToken)
                .ConfigureAwait(false);
            if (message is null)
            {
                return Error.NotFound("email_message_not_found", "Message not found.");
            }

            // A message is only visible to those who may use its account; anything else looks like it does not exist.
            var account = await EmailAccountAccess.FindAsync(db, message.AccountId, query.UserId, cancellationToken).ConfigureAwait(false);
            if (!account.IsSuccess)
            {
                return Error.NotFound("email_message_not_found", "Message not found.");
            }

            var attachments = await AttachmentQueries.OfReceivedAsync(db, message.Id, cancellationToken).ConfigureAwait(false);
            return EmailMessageDto.Full(message, attachments);
        }
    }
}

using Dotnetstore.PersonalApp.Modules.Email.Domain;
using Dotnetstore.PersonalApp.Modules.Email.Features.EmailAccounts;
using Dotnetstore.PersonalApp.Modules.Email.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using Microsoft.EntityFrameworkCore;
using Dotnetstore.PersonalApp.Shared.Telemetry;

namespace Dotnetstore.PersonalApp.Modules.Email.Features.Messages;

/// <summary>
/// Deletes a downloaded message (and its attachments) from the local inbox only. The server keeps its copy, so the message's id is
/// remembered and later fetches do not bring it back.
/// </summary>
internal static class DeleteEmailMessage
{
    public sealed record Command(Guid Id, Guid UserId);

    public sealed class Handler(IDbContextFactory<EmailDbContext> dbFactory)
    {
        public Task<Result<Guid>> HandleAsync(Command command, CancellationToken cancellationToken) =>
            AppTelemetry.TraceAsync(this, () => HandleCoreAsync(command, cancellationToken), command);

        private async Task<Result<Guid>> HandleCoreAsync(Command command, CancellationToken cancellationToken)
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            var message = await db.Messages.FirstOrDefaultAsync(m => m.Id == command.Id, cancellationToken).ConfigureAwait(false);
            var account = message is null
                ? null
                : await EmailAccountAccess.FindAsync(db, message.AccountId, command.UserId, cancellationToken).ConfigureAwait(false);
            if (message is null || account is null || !account.IsSuccess)
            {
                return Error.NotFound("email_message_not_found", "Message not found.");
            }

            var remembered = await db.DeletedMessages
                .AnyAsync(d => d.AccountId == message.AccountId && d.Uid == message.Uid, cancellationToken)
                .ConfigureAwait(false);
            if (!remembered)
            {
                db.DeletedMessages.Add(new DeletedMessage { Id = Guid.CreateVersion7(), AccountId = message.AccountId, Uid = message.Uid });
            }

            db.Messages.Remove(message);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return command.Id;
        }
    }
}

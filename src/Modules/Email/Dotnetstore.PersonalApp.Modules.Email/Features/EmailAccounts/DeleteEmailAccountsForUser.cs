using Dotnetstore.PersonalApp.Modules.Email.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using Microsoft.EntityFrameworkCore;
using Dotnetstore.PersonalApp.Shared.Telemetry;

namespace Dotnetstore.PersonalApp.Modules.Email.Features.EmailAccounts;

/// <summary>Deletes every personal account of one user (never the system accounts). Having none is not an error: the result is the number deleted.</summary>
internal static class DeleteEmailAccountsForUser
{
    public sealed record Command(Guid UserId);

    public sealed class Handler(IDbContextFactory<EmailDbContext> dbFactory)
    {
        public Task<Result<int>> HandleAsync(Command command, CancellationToken cancellationToken) =>
            AppTelemetry.TraceAsync(this, () => HandleCoreAsync(command, cancellationToken), command);

        private async Task<Result<int>> HandleCoreAsync(Command command, CancellationToken cancellationToken)
        {
            if (command.UserId == Guid.Empty)
            {
                return Error.Validation("email_account_user_required", "Nobody is signed in.");
            }

            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            // Drafts are personal too (their attachments go with them).
            await db.Drafts.Where(d => d.UserId == command.UserId).ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);

            return await db.Accounts
                .Where(a => a.OwnerId == command.UserId)
                .ExecuteDeleteAsync(cancellationToken)
                .ConfigureAwait(false);
        }
    }
}

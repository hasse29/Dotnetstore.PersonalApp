using Dotnetstore.PersonalApp.Modules.Email.Domain;
using Dotnetstore.PersonalApp.Modules.Email.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using Microsoft.EntityFrameworkCore;
using Dotnetstore.PersonalApp.Shared.Telemetry;

namespace Dotnetstore.PersonalApp.Modules.Email.Features.EmailAccounts;

internal static class GetEmailAccount
{
    public sealed record Query(Guid Id, Guid UserId);

    public sealed class Handler(IDbContextFactory<EmailDbContext> dbFactory)
    {
        public Task<Result<EmailAccountDto>> HandleAsync(Query query, CancellationToken cancellationToken) =>
            AppTelemetry.TraceAsync(this, () => HandleCoreAsync(query, cancellationToken), query);

        private async Task<Result<EmailAccountDto>> HandleCoreAsync(Query query, CancellationToken cancellationToken)
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            var account = await db.Accounts.AsNoTracking()
                .FirstOrDefaultAsync(
                    a => a.Id == query.Id && (a.OwnerId == query.UserId || a.OwnerId == EmailAccount.SystemOwner),
                    cancellationToken)
                .ConfigureAwait(false);

            return account is null ? Error.NotFound("email_account_not_found", "Account not found.") : EmailAccountDto.From(account);
        }
    }
}

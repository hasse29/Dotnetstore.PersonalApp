using Dotnetstore.PersonalApp.Modules.Email.Domain;
using Dotnetstore.PersonalApp.Modules.Email.Persistence;
using Microsoft.EntityFrameworkCore;
using Dotnetstore.PersonalApp.Shared.Telemetry;

namespace Dotnetstore.PersonalApp.Modules.Email.Features.EmailAccounts;

/// <summary>Lists the accounts a user can use: the user's own and the system accounts. There is deliberately no "list everyone's accounts" query.</summary>
internal static class ListEmailAccounts
{
    public const int MaxPageSize = 50;

    public sealed record Query(Guid UserId, int Page = 1, int PageSize = MaxPageSize);

    public sealed record Response(IReadOnlyList<EmailAccountDto> Items, int TotalCount);

    public sealed class Handler(IDbContextFactory<EmailDbContext> dbFactory)
    {
        public Task<Response> HandleAsync(Query query, CancellationToken cancellationToken) =>
            AppTelemetry.TraceAsync(this, () => HandleCoreAsync(query, cancellationToken), query);

        private async Task<Response> HandleCoreAsync(Query query, CancellationToken cancellationToken)
        {
            var page = Math.Max(query.Page, 1);
            var pageSize = Math.Clamp(query.PageSize, 1, MaxPageSize);

            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            var accounts = db.Accounts.AsNoTracking()
                .Where(a => a.OwnerId == query.UserId || a.OwnerId == EmailAccount.SystemOwner);

            var total = await accounts.CountAsync(cancellationToken).ConfigureAwait(false);
            var rows = await accounts
                .OrderBy(a => a.OwnerId == EmailAccount.SystemOwner)
                .ThenBy(a => a.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            return new Response(rows.ConvertAll(EmailAccountDto.From), total);
        }
    }
}

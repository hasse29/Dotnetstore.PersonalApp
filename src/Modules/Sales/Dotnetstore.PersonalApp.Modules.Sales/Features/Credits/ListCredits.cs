using Dotnetstore.PersonalApp.Modules.Sales.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Sales.Features.Credits;

/// <summary>The customers who have credit left, the largest first.</summary>
internal static class ListCredits
{
    public sealed class Handler(IDbContextFactory<SalesDbContext> dbFactory)
    {
        public async Task<IReadOnlyList<CustomerCreditDto>> HandleAsync(CancellationToken cancellationToken)
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            // Summed in memory: SQLite cannot sum a decimal column.
            var entries = await db.CreditEntries.AsNoTracking()
                .Select(e => new { e.CustomerId, e.Amount })
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            var balances = entries
                .GroupBy(e => e.CustomerId)
                .Select(g => (CustomerId: g.Key, Balance: g.Sum(e => e.Amount)))
                .Where(b => b.Balance > 0m)
                .ToList();

            var ids = balances.Select(b => b.CustomerId).ToList();
            var names = await db.Customers.AsNoTracking()
                .Where(c => ids.Contains(c.Id))
                .Select(c => new { c.Id, c.Name })
                .ToDictionaryAsync(c => c.Id, c => c.Name, cancellationToken)
                .ConfigureAwait(false);

            return [.. balances
                .Select(b => new CustomerCreditDto(b.CustomerId, names.GetValueOrDefault(b.CustomerId, string.Empty), b.Balance))
                .OrderByDescending(c => c.Balance)
                .ThenBy(c => c.CustomerName, StringComparer.CurrentCultureIgnoreCase)];
        }
    }
}

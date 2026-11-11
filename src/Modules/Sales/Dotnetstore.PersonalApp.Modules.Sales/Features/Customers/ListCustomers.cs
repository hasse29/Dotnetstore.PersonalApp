using Dotnetstore.PersonalApp.Modules.Sales.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Sales.Features.Customers;

internal static class ListCustomers
{
    public const int MaxPageSize = 50;

    /// <param name="ActiveOnly">True for pickers: inactive customers are not offered for new offers.</param>
    public sealed record Query(string? Search = null, bool ActiveOnly = false, int Page = 1, int PageSize = MaxPageSize);

    public sealed record Response(IReadOnlyList<CustomerDto> Items, int TotalCount);

    public sealed class Handler(IDbContextFactory<SalesDbContext> dbFactory)
    {
        public async Task<Response> HandleAsync(Query query, CancellationToken cancellationToken)
        {
            var page = Math.Max(query.Page, 1);
            var pageSize = Math.Clamp(query.PageSize, 1, MaxPageSize);

            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            var customers = db.Customers.AsNoTracking();
            if (query.ActiveOnly)
            {
                customers = customers.Where(c => c.IsActive);
            }

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var pattern = $"%{query.Search.Trim()}%";
                customers = customers.Where(c =>
                    EF.Functions.Like(c.Name, pattern) ||
                    (c.OrganizationNumber != null && EF.Functions.Like(c.OrganizationNumber, pattern)) ||
                    (c.Email != null && EF.Functions.Like(c.Email, pattern)) ||
                    (c.City != null && EF.Functions.Like(c.City, pattern)));
            }

            var total = await customers.CountAsync(cancellationToken).ConfigureAwait(false);
            var items = await customers
                .OrderBy(c => c.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(c => new CustomerDto(c.Id, c.Name, c.OrganizationNumber, c.Email, c.Phone, c.Street, c.PostalCode, c.City, c.Notes, c.IsActive))
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            return new Response(items, total);
        }
    }
}

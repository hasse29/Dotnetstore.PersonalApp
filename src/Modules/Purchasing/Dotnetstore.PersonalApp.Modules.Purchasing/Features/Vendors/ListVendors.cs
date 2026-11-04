using Dotnetstore.PersonalApp.Modules.Purchasing.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Purchasing.Features.Vendors;

internal static class ListVendors
{
    public const int MaxPageSize = 50;

    public sealed record Query(string? Search = null, int Page = 1, int PageSize = MaxPageSize);

    public sealed record Response(IReadOnlyList<VendorDto> Items, int TotalCount);

    public sealed class Handler(IDbContextFactory<PurchasingDbContext> dbFactory, ExternalReferences references)
    {
        public async Task<Response> HandleAsync(Query query, CancellationToken cancellationToken)
        {
            var page = Math.Max(query.Page, 1);
            var pageSize = Math.Clamp(query.PageSize, 1, MaxPageSize);

            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            var rows = db.Vendors.AsNoTracking();
            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var pattern = $"%{query.Search.Trim()}%";
                rows = rows.Where(t =>
                    EF.Functions.Like(t.AccountNumber, pattern) ||
                    EF.Functions.Like(t.Name, pattern) ||
                    (t.PurchasingWebServiceUrl != null && EF.Functions.Like(t.PurchasingWebServiceUrl, pattern)));
            }

            var total = await rows.CountAsync(cancellationToken).ConfigureAwait(false);
            var items = await rows
                .OrderBy(t => t.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(VendorDto.Projection)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            var clientNames = await references.ClientNamesAsync(cancellationToken).ConfigureAwait(false);
            IReadOnlyList<VendorDto> named = [.. items.Select(i => i.WithNames(clientNames))];

            return new Response(named, total);
        }
    }
}
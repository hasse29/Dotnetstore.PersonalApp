using Dotnetstore.PersonalApp.Modules.Products.Persistence;
using Microsoft.EntityFrameworkCore;
using Dotnetstore.PersonalApp.Shared.Telemetry;

namespace Dotnetstore.PersonalApp.Modules.Products.Features.Suppliers;

internal static class ListSuppliers
{
    public const int MaxPageSize = 50;

    /// <param name="ActiveOnly">True for pickers: inactive suppliers are not offered for new prices.</param>
    public sealed record Query(string? Search = null, bool ActiveOnly = false, int Page = 1, int PageSize = MaxPageSize);

    public sealed record Response(IReadOnlyList<SupplierDto> Items, int TotalCount);

    public sealed class Handler(IDbContextFactory<ProductsDbContext> dbFactory)
    {
        public Task<Response> HandleAsync(Query query, CancellationToken cancellationToken) =>
            AppTelemetry.TraceAsync(this, () => HandleCoreAsync(query, cancellationToken), query);

        private async Task<Response> HandleCoreAsync(Query query, CancellationToken cancellationToken)
        {
            var page = Math.Max(query.Page, 1);
            var pageSize = Math.Clamp(query.PageSize, 1, MaxPageSize);

            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            var suppliers = db.Suppliers.AsNoTracking();
            if (query.ActiveOnly)
            {
                suppliers = suppliers.Where(s => s.IsActive);
            }

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var pattern = $"%{query.Search.Trim()}%";
                suppliers = suppliers.Where(s =>
                    EF.Functions.Like(s.Name, pattern) ||
                    (s.OrganizationNumber != null && EF.Functions.Like(s.OrganizationNumber, pattern)) ||
                    (s.Email != null && EF.Functions.Like(s.Email, pattern)));
            }

            var total = await suppliers.CountAsync(cancellationToken).ConfigureAwait(false);
            var items = await suppliers
                .OrderBy(s => s.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(s => new SupplierDto(s.Id, s.Name, s.OrganizationNumber, s.Email, s.Phone, s.Website, s.Notes, s.IsActive))
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            return new Response(items, total);
        }
    }
}

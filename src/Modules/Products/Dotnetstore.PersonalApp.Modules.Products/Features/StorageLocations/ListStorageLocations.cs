using Dotnetstore.PersonalApp.Modules.Products.Persistence;
using Microsoft.EntityFrameworkCore;
using Dotnetstore.PersonalApp.Shared.Telemetry;

namespace Dotnetstore.PersonalApp.Modules.Products.Features.StorageLocations;

internal static class ListStorageLocations
{
    public const int MaxPageSize = 100;

    /// <param name="ActiveOnly">True for pickers that register new stock.</param>
    public sealed record Query(string? Search = null, bool ActiveOnly = false, int Page = 1, int PageSize = MaxPageSize);

    public sealed record Response(IReadOnlyList<StorageLocationDto> Items, int TotalCount);

    public sealed class Handler(IDbContextFactory<ProductsDbContext> dbFactory)
    {
        public Task<Response> HandleAsync(Query query, CancellationToken cancellationToken) =>
            AppTelemetry.TraceAsync(this, () => HandleCoreAsync(query, cancellationToken), query);

        private async Task<Response> HandleCoreAsync(Query query, CancellationToken cancellationToken)
        {
            var page = Math.Max(query.Page, 1);
            var pageSize = Math.Clamp(query.PageSize, 1, MaxPageSize);

            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            var locations = db.StorageLocations.AsNoTracking();
            if (query.ActiveOnly)
            {
                locations = locations.Where(l => l.IsActive);
            }

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var pattern = $"%{query.Search.Trim()}%";
                locations = locations.Where(l =>
                    EF.Functions.Like(l.Name, pattern) ||
                    (l.Description != null && EF.Functions.Like(l.Description, pattern)));
            }

            var total = await locations.CountAsync(cancellationToken).ConfigureAwait(false);
            var items = await locations
                .OrderBy(l => l.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(l => new StorageLocationDto(l.Id, l.Name, l.Description, l.IsActive))
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            return new Response(items, total);
        }
    }
}

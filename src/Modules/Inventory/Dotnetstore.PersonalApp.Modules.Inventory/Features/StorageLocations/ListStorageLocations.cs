using Dotnetstore.PersonalApp.Modules.Inventory.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Inventory.Features.StorageLocations;

internal static class ListStorageLocations
{
    public const int MaxPageSize = 50;

    public sealed record Query(string? Search = null, int Page = 1, int PageSize = MaxPageSize, Guid? ParentId = null);

    public sealed record Response(IReadOnlyList<StorageLocationDto> Items, int TotalCount);

    public sealed class Handler(IDbContextFactory<InventoryDbContext> dbFactory)
    {
        public async Task<Response> HandleAsync(Query query, CancellationToken cancellationToken)
        {
            var page = Math.Max(query.Page, 1);
            var pageSize = Math.Clamp(query.PageSize, 1, MaxPageSize);

            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            var rows = db.StorageLocations.AsNoTracking();

            if (query.ParentId is { } parentId)
            {
                rows = rows.Where(t => t.WarehouseId == parentId);
            }

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var pattern = $"%{query.Search.Trim()}%";
                rows = rows.Where(t =>
                    EF.Functions.Like(t.Code, pattern) ||
                    (t.Zone != null && EF.Functions.Like(t.Zone, pattern)) ||
                    (t.Description != null && EF.Functions.Like(t.Description, pattern)) ||
                    EF.Functions.Like(t.Warehouse.Name, pattern));
            }

            var total = await rows.CountAsync(cancellationToken).ConfigureAwait(false);
            var items = await rows
                .OrderBy(t => t.Code)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(StorageLocationDto.Projection)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            return new Response(items, total);
        }
    }
}
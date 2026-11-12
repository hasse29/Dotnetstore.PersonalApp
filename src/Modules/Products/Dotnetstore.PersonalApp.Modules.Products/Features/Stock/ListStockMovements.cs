using Dotnetstore.PersonalApp.Modules.Products.Domain;
using Dotnetstore.PersonalApp.Modules.Products.Persistence;
using Microsoft.EntityFrameworkCore;
using Dotnetstore.PersonalApp.Shared.Telemetry;

namespace Dotnetstore.PersonalApp.Modules.Products.Features.Stock;

internal sealed record StockMovementDto(
    Guid Id,
    DateTimeOffset OccurredAt,
    Guid ProductId,
    string ProductNumber,
    string ProductName,
    Guid LocationId,
    string LocationName,
    StockMovementType Type,
    decimal Quantity,
    string? SupplierName,
    string? Note);

/// <summary>The stock history, newest first.</summary>
internal static class ListStockMovements
{
    public const int MaxPageSize = 50;

    public sealed record Query(string? Search = null, StockMovementType? Type = null, Guid? ProductId = null, int Page = 1, int PageSize = MaxPageSize);

    public sealed record Response(IReadOnlyList<StockMovementDto> Items, int TotalCount);

    public sealed class Handler(IDbContextFactory<ProductsDbContext> dbFactory)
    {
        public Task<Response> HandleAsync(Query query, CancellationToken cancellationToken) =>
            AppTelemetry.TraceAsync(this, () => HandleCoreAsync(query, cancellationToken), query);

        private async Task<Response> HandleCoreAsync(Query query, CancellationToken cancellationToken)
        {
            var page = Math.Max(query.Page, 1);
            var pageSize = Math.Clamp(query.PageSize, 1, MaxPageSize);

            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            var movements = db.StockMovements.AsNoTracking();
            if (query.Type is { } type)
            {
                movements = movements.Where(m => m.Type == type);
            }

            if (query.ProductId is { } productId)
            {
                movements = movements.Where(m => m.ProductId == productId);
            }

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var pattern = $"%{query.Search.Trim()}%";
                movements = movements.Where(m => db.Products.Any(p =>
                    p.Id == m.ProductId && (EF.Functions.Like(p.Number, pattern) || EF.Functions.Like(p.Name, pattern))));
            }

            var total = await movements.CountAsync(cancellationToken).ConfigureAwait(false);

            // Ids are time-ordered (version 7), and SQLite cannot order by DateTimeOffset.
            var items = await movements
                .OrderByDescending(m => m.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(m => new StockMovementDto(
                    m.Id,
                    m.OccurredAt,
                    m.ProductId,
                    db.Products.Where(p => p.Id == m.ProductId).Select(p => p.Number).First(),
                    db.Products.Where(p => p.Id == m.ProductId).Select(p => p.Name).First(),
                    m.LocationId,
                    db.StorageLocations.Where(l => l.Id == m.LocationId).Select(l => l.Name).First(),
                    m.Type,
                    m.Quantity,
                    db.Suppliers.Where(s => s.Id == m.SupplierId).Select(s => s.Name).FirstOrDefault(),
                    m.Note))
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            return new Response(items, total);
        }
    }
}

using Dotnetstore.PersonalApp.Shared.Results;
using Dotnetstore.PersonalApp.Shared.Stock;

namespace Dotnetstore.PersonalApp.Modules.Sales.Tests;

/// <summary>Stock per product and location in memory, taking from the location with the most first, like the Products module.</summary>
internal sealed class FakeStockService : IStockService
{
    private readonly Dictionary<(Guid Product, Guid Location), decimal> _stock = [];

    public void Add(Guid productId, Guid locationId, decimal quantity) => _stock[(productId, locationId)] = quantity;

    public decimal Total(Guid productId) => _stock.Where(s => s.Key.Product == productId).Sum(s => s.Value);

    public decimal At(Guid productId, Guid locationId) => _stock.GetValueOrDefault((productId, locationId));

    public Task<Result<IReadOnlyList<StockAllocation>>> IssueAsync(Guid productId, decimal quantity, string note, CancellationToken cancellationToken)
    {
        if (Total(productId) < quantity)
        {
            return Task.FromResult<Result<IReadOnlyList<StockAllocation>>>(Error.Conflict("stock_insufficient_total", "Not enough in stock."));
        }

        var allocations = new List<StockAllocation>();
        var remaining = quantity;
        foreach (var (key, available) in _stock.Where(s => s.Key.Product == productId && s.Value > 0).OrderByDescending(s => s.Value).ToList())
        {
            if (remaining <= 0)
            {
                break;
            }

            var take = Math.Min(available, remaining);
            _stock[key] = available - take;
            allocations.Add(new StockAllocation(key.Location, take));
            remaining -= take;
        }

        return Task.FromResult<Result<IReadOnlyList<StockAllocation>>>(allocations);
    }

    public Task<Result<bool>> ReturnAsync(Guid productId, IReadOnlyList<StockAllocation> allocations, string note, CancellationToken cancellationToken)
    {
        foreach (var allocation in allocations)
        {
            _stock[(productId, allocation.LocationId)] = At(productId, allocation.LocationId) + allocation.Quantity;
        }

        return Task.FromResult<Result<bool>>(true);
    }
}

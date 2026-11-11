using Dotnetstore.PersonalApp.Shared.Results;

namespace Dotnetstore.PersonalApp.Shared.Stock;

/// <summary>How much of an issue came from one storage location, so that it can be put back there.</summary>
public sealed record StockAllocation(Guid LocationId, decimal Quantity);

/// <summary>
/// Implemented by the Products module so that other modules can take goods out of stock, and put them back, without referencing it.
/// The module chooses the locations; the caller keeps the returned allocations if it may have to undo the issue.
/// </summary>
public interface IStockService
{
    /// <summary>
    /// Takes <paramref name="quantity"/> of the product out of stock, from the locations with the most of it first. Fails, changing
    /// nothing, when there is not enough in total (<c>stock_insufficient_total</c>) or the product cannot be in stock.
    /// </summary>
    Task<Result<IReadOnlyList<StockAllocation>>> IssueAsync(Guid productId, decimal quantity, string note, CancellationToken cancellationToken);

    /// <summary>Puts goods that were issued back at the locations they came from.</summary>
    Task<Result<bool>> ReturnAsync(Guid productId, IReadOnlyList<StockAllocation> allocations, string note, CancellationToken cancellationToken);
}

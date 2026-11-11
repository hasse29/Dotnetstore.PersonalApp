namespace Dotnetstore.PersonalApp.Shared.Products;

/// <summary>A product as other modules see it: what they need to put it on a document. <paramref name="Unit"/> is a unit key such as Piece or Hour; the price excludes VAT. <paramref name="IsStockable"/> is false for consulting hours, which are never picked.</summary>
public sealed record ProductSummary(Guid Id, string Number, string Name, string Unit, decimal UnitPrice, decimal VatRate, bool IsStockable);

/// <summary>Implemented by the Products module so that other modules can pick products for offers and orders without referencing it.</summary>
public interface IProductCatalog
{
    /// <summary>Active products matching the text in number or name, ordered by name, at most <paramref name="maxCount"/>.</summary>
    Task<IReadOnlyList<ProductSummary>> SearchAsync(string? search, int maxCount, CancellationToken cancellationToken);
}

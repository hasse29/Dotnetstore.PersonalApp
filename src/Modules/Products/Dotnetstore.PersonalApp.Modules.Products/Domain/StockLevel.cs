namespace Dotnetstore.PersonalApp.Modules.Products.Domain;

/// <summary>How many of a product there are at one location. Kept in step with <see cref="StockMovement"/> by the stock ledger.</summary>
internal sealed class StockLevel
{
    public Guid Id { get; set; }

    public Guid ProductId { get; set; }

    public Guid LocationId { get; set; }

    /// <summary>Never negative.</summary>
    public decimal Quantity { get; set; }
}

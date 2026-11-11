namespace Dotnetstore.PersonalApp.Modules.Products.Domain;

internal sealed class InventoryCountLine
{
    public Guid Id { get; set; }

    public Guid CountId { get; set; }

    public Guid ProductId { get; set; }

    public Guid LocationId { get; set; }

    /// <summary>What the system said when the count was made; refreshed when the count is posted.</summary>
    public decimal ExpectedQuantity { get; set; }

    /// <summary>Null until it has been counted; uncounted lines are left alone when posting.</summary>
    public decimal? CountedQuantity { get; set; }
}

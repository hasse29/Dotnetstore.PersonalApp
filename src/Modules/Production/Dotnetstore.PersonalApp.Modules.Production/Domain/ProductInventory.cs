namespace Dotnetstore.PersonalApp.Modules.Production.Domain;

/// <summary>inventory entry.</summary>
internal sealed class ProductInventory
{
    public Guid Id { get; set; }

    public Guid ProductId { get; set; }

    public Product Product { get; set; } = null!;

    public Guid LocationId { get; set; }

    public Location Location { get; set; } = null!;

    public string Shelf { get; set; } = string.Empty;

    public int Bin { get; set; }

    public int Quantity { get; set; }
}
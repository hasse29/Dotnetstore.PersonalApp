namespace Dotnetstore.PersonalApp.Modules.Products.Domain;

/// <summary>
/// Something that is sold: hardware, software the owner makes, or consulting hours. The kind-specific fields are only kept for
/// their own kind (<see cref="Manufacturer"/> and <see cref="Model"/> for hardware, <see cref="SoftwareVersion"/> for software,
/// <see cref="MinimumHours"/> for consulting).
/// </summary>
internal sealed class Product
{
    public Guid Id { get; set; }

    /// <summary>The owner's own code for the product (SKU); unique, ignoring case.</summary>
    public string Number { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public ProductKind Kind { get; set; }

    public Guid? CategoryId { get; set; }

    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public ProductUnit Unit { get; set; }

    /// <summary>The selling price per <see cref="Unit"/>, excluding VAT. For consulting this is the hourly rate.</summary>
    public decimal UnitPrice { get; set; }

    /// <summary>What the product costs to buy or make, per <see cref="Unit"/>, excluding VAT.</summary>
    public decimal CostPrice { get; set; }

    /// <summary>VAT in percent, 0 to 100.</summary>
    public decimal VatRate { get; set; }

    public string? Manufacturer { get; set; }

    public string? Model { get; set; }

    public string? SoftwareVersion { get; set; }

    public decimal? MinimumHours { get; set; }

    /// <summary>The stock level at or below which the product counts as low; null for no limit. Not kept for consulting, which has no stock.</summary>
    public decimal? MinimumStock { get; set; }

    /// <summary>The product picture (PNG or JPEG); read and written only through the photo handlers.</summary>
    public byte[]? Photo { get; set; }

    /// <summary>A small square copy of <see cref="Photo"/> for lists.</summary>
    public byte[]? PhotoThumbnail { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}

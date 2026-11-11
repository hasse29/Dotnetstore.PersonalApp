namespace Dotnetstore.PersonalApp.Modules.Products.Domain;

/// <summary>What one supplier charges for one product. A supplier has at most one price per product.</summary>
internal sealed class SupplierPrice
{
    public Guid Id { get; set; }

    public Guid SupplierId { get; set; }

    public Guid ProductId { get; set; }

    /// <summary>The supplier's own article number for the product.</summary>
    public string? SupplierArticleNumber { get; set; }

    /// <summary>The purchase price per <see cref="Product.Unit"/>, excluding VAT.</summary>
    public decimal Price { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}

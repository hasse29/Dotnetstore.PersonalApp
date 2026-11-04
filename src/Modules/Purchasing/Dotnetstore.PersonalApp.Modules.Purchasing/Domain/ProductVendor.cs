namespace Dotnetstore.PersonalApp.Modules.Purchasing.Domain;

/// <summary>vendor price.</summary>
internal sealed class ProductVendor
{
    public Guid Id { get; set; }

    public Guid ProductId { get; set; }

    public Guid VendorId { get; set; }

    public Vendor Vendor { get; set; } = null!;

    public int AverageLeadTime { get; set; }

    public decimal StandardPrice { get; set; }

    public decimal? LastReceiptCost { get; set; }

    public DateOnly? LastReceiptDate { get; set; }

    public int MinOrderQty { get; set; }

    public int MaxOrderQty { get; set; }

    public int? OnOrderQty { get; set; }

    public Guid UnitId { get; set; }
}
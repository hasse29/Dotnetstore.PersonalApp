namespace Dotnetstore.PersonalApp.Modules.Purchasing.Domain;

/// <summary>purchase order.</summary>
internal sealed class PurchaseOrder
{
    public Guid Id { get; set; }

    public string Number { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public Guid? BuyerId { get; set; }

    public Guid VendorId { get; set; }

    public Vendor Vendor { get; set; } = null!;

    public Guid ShipMethodId { get; set; }

    public ShipMethod ShipMethod { get; set; } = null!;

    public DateOnly OrderDate { get; set; }

    public DateOnly? ShipDate { get; set; }

    public decimal TaxAmt { get; set; }

    public decimal Freight { get; set; }

    public decimal SubTotal { get; set; }

    public decimal TotalDue { get; set; }
}
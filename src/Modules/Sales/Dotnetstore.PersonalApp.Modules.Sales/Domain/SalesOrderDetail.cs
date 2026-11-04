namespace Dotnetstore.PersonalApp.Modules.Sales.Domain;

/// <summary>sales order line.</summary>
internal sealed class SalesOrderDetail
{
    public Guid Id { get; set; }

    public Guid SalesOrderId { get; set; }

    public SalesOrder SalesOrder { get; set; } = null!;

    public Guid ProductId { get; set; }

    public string? CarrierTrackingNumber { get; set; }

    public int OrderQty { get; set; }

    public Guid? SpecialOfferId { get; set; }

    public SpecialOffer? SpecialOffer { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal UnitPriceDiscount { get; set; }

    public decimal LineTotal { get; set; }
}
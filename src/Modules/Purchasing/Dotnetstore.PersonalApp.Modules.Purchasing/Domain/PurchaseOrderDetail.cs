namespace Dotnetstore.PersonalApp.Modules.Purchasing.Domain;

/// <summary>purchase order line.</summary>
internal sealed class PurchaseOrderDetail
{
    public Guid Id { get; set; }

    public Guid PurchaseOrderId { get; set; }

    public PurchaseOrder PurchaseOrder { get; set; } = null!;

    public Guid ProductId { get; set; }

    public DateOnly DueDate { get; set; }

    public int OrderQty { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal ReceivedQty { get; set; }

    public decimal RejectedQty { get; set; }

    public decimal LineTotal { get; set; }

    public decimal StockedQty { get; set; }
}
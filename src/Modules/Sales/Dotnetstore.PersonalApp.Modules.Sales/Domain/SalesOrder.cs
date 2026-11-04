namespace Dotnetstore.PersonalApp.Modules.Sales.Domain;

/// <summary>sales order.</summary>
internal sealed class SalesOrder
{
    public Guid Id { get; set; }

    public string Number { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public bool OnlineOrderFlag { get; set; }

    public DateOnly OrderDate { get; set; }

    public DateOnly DueDate { get; set; }

    public DateOnly? ShipDate { get; set; }

    public string? PurchaseOrderNumber { get; set; }

    public Guid CustomerId { get; set; }

    public Customer Customer { get; set; } = null!;

    public Guid? SalesPersonId { get; set; }

    public SalesPerson? SalesPerson { get; set; }

    public Guid? TerritoryId { get; set; }

    public SalesTerritory? Territory { get; set; }

    public Guid? ShipMethodId { get; set; }

    public decimal TaxAmt { get; set; }

    public decimal Freight { get; set; }

    public string? Comment { get; set; }

    public decimal SubTotal { get; set; }

    public decimal TotalDue { get; set; }
}
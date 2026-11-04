namespace Dotnetstore.PersonalApp.Modules.Sales.Domain;

/// <summary>order reason.</summary>
internal sealed class SalesOrderReason
{
    public Guid Id { get; set; }

    public Guid SalesOrderId { get; set; }

    public SalesOrder SalesOrder { get; set; } = null!;

    public Guid SalesReasonId { get; set; }

    public SalesReason SalesReason { get; set; } = null!;
}
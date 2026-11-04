using System.Linq.Expressions;
using Dotnetstore.PersonalApp.Modules.Sales.Domain;
using Dotnetstore.PersonalApp.Modules.Sales.Features;

namespace Dotnetstore.PersonalApp.Modules.Sales.Features.SalesOrders;

internal sealed record SalesOrderDto(Guid Id, string Number, string Status, bool OnlineOrderFlag, DateOnly OrderDate, DateOnly DueDate, DateOnly? ShipDate, string? PurchaseOrderNumber, Guid CustomerId, string CustomerName, Guid? SalesPersonId, string? SalesPersonName, Guid? TerritoryId, string? TerritoryName, Guid? ShipMethodId, string? ShipMethodName, decimal TaxAmt, decimal Freight, string? Comment, decimal SubTotal, decimal TotalDue)
{
    public static Expression<Func<SalesOrder, SalesOrderDto>> Projection { get; } =
        t => new SalesOrderDto(t.Id, t.Number, t.Status, t.OnlineOrderFlag, t.OrderDate, t.DueDate, t.ShipDate, t.PurchaseOrderNumber, t.CustomerId, t.Customer.Name, t.SalesPersonId, t.SalesPerson == null ? null : t.SalesPerson.Name, t.TerritoryId, t.Territory == null ? null : t.Territory.Name, t.ShipMethodId, null, t.TaxAmt, t.Freight, t.Comment, t.SubTotal, t.TotalDue);

    public SalesOrderDto WithNames(IReadOnlyDictionary<Guid, string> shipMethodNames) => this with { ShipMethodName = ExternalReferences.NameOf(shipMethodNames, ShipMethodId) };
}
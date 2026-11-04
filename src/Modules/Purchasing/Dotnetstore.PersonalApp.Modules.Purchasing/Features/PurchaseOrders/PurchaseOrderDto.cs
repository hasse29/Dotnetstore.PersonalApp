using System.Linq.Expressions;
using Dotnetstore.PersonalApp.Modules.Purchasing.Domain;
using Dotnetstore.PersonalApp.Modules.Purchasing.Features;

namespace Dotnetstore.PersonalApp.Modules.Purchasing.Features.PurchaseOrders;

internal sealed record PurchaseOrderDto(Guid Id, string Number, string Status, Guid? BuyerId, string? BuyerName, Guid VendorId, string VendorName, Guid ShipMethodId, string ShipMethodName, DateOnly OrderDate, DateOnly? ShipDate, decimal TaxAmt, decimal Freight, decimal SubTotal, decimal TotalDue)
{
    public static Expression<Func<PurchaseOrder, PurchaseOrderDto>> Projection { get; } =
        t => new PurchaseOrderDto(t.Id, t.Number, t.Status, t.BuyerId, null, t.VendorId, t.Vendor.Name, t.ShipMethodId, t.ShipMethod.Name, t.OrderDate, t.ShipDate, t.TaxAmt, t.Freight, t.SubTotal, t.TotalDue);

    public PurchaseOrderDto WithNames(IReadOnlyDictionary<Guid, string> userNames) => this with { BuyerName = ExternalReferences.NameOf(userNames, BuyerId) };
}
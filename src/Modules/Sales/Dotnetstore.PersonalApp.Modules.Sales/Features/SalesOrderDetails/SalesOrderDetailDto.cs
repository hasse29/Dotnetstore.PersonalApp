using System.Linq.Expressions;
using Dotnetstore.PersonalApp.Modules.Sales.Domain;
using Dotnetstore.PersonalApp.Modules.Sales.Features;

namespace Dotnetstore.PersonalApp.Modules.Sales.Features.SalesOrderDetails;

internal sealed record SalesOrderDetailDto(Guid Id, Guid SalesOrderId, string SalesOrderName, Guid ProductId, string ProductName, string? CarrierTrackingNumber, int OrderQty, Guid? SpecialOfferId, string? SpecialOfferName, decimal UnitPrice, decimal UnitPriceDiscount, decimal LineTotal, int ShippedQty)
{
    public static Expression<Func<SalesOrderDetail, SalesOrderDetailDto>> Projection { get; } =
        t => new SalesOrderDetailDto(t.Id, t.SalesOrderId, t.SalesOrder.Number, t.ProductId, string.Empty, t.CarrierTrackingNumber, t.OrderQty, t.SpecialOfferId, t.SpecialOffer == null ? null : t.SpecialOffer.Description, t.UnitPrice, t.UnitPriceDiscount, t.LineTotal, t.ShippedQty);

    public SalesOrderDetailDto WithNames(IReadOnlyDictionary<Guid, string> productNames) => this with { ProductName = productNames.GetValueOrDefault(ProductId, string.Empty) };
}
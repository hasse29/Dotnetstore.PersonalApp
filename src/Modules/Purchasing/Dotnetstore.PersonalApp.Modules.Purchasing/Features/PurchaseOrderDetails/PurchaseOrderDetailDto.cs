using System.Linq.Expressions;
using Dotnetstore.PersonalApp.Modules.Purchasing.Domain;
using Dotnetstore.PersonalApp.Modules.Purchasing.Features;

namespace Dotnetstore.PersonalApp.Modules.Purchasing.Features.PurchaseOrderDetails;

internal sealed record PurchaseOrderDetailDto(Guid Id, Guid PurchaseOrderId, string PurchaseOrderName, Guid ProductId, string ProductName, DateOnly DueDate, int OrderQty, decimal UnitPrice, decimal ReceivedQty, decimal RejectedQty, decimal LineTotal, decimal StockedQty)
{
    public static Expression<Func<PurchaseOrderDetail, PurchaseOrderDetailDto>> Projection { get; } =
        t => new PurchaseOrderDetailDto(t.Id, t.PurchaseOrderId, t.PurchaseOrder.Number, t.ProductId, string.Empty, t.DueDate, t.OrderQty, t.UnitPrice, t.ReceivedQty, t.RejectedQty, t.LineTotal, t.StockedQty);

    public PurchaseOrderDetailDto WithNames(IReadOnlyDictionary<Guid, string> productNames) => this with { ProductName = productNames.GetValueOrDefault(ProductId, string.Empty) };
}
using Dotnetstore.PersonalApp.Modules.Purchasing.Features.ProductVendors;
using Dotnetstore.PersonalApp.Modules.Purchasing.Features.PurchaseOrderDetails;
using Dotnetstore.PersonalApp.Modules.Purchasing.Features.PurchaseOrders;
using Dotnetstore.PersonalApp.Shared.Contracts.Events;
using Dotnetstore.PersonalApp.Shared.Events;
using Microsoft.Extensions.DependencyInjection;

namespace Dotnetstore.PersonalApp.Modules.Purchasing.Tests;

public sealed class OrderTotalsTests : IDisposable
{
    private readonly PurchasingFixture _fixture = new();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public void Dispose() => _fixture.Dispose();

    private async Task<PurchaseOrderDto> OrderAsync(Guid id)
    {
        var all = await _fixture.Services.GetRequiredService<ListPurchaseOrders.Handler>().HandleAsync(new ListPurchaseOrders.Query(), Ct);
        return all.Items.Single(o => o.Id == id);
    }

    private async Task<PurchaseOrderDetailDto> AddLineAsync(Guid orderId, int quantity, decimal unitPrice)
    {
        var command = await _fixture.PurchaseOrderDetailCommandAsync(Ct);
        var result = await _fixture.Services.GetRequiredService<CreatePurchaseOrderDetail.Handler>()
            .HandleAsync(command with { PurchaseOrderId = orderId, OrderQty = quantity, UnitPrice = unitPrice, ReceivedQty = 5m, RejectedQty = 2m }, Ct);
        return result.Value;
    }

    [Fact]
    public async Task LinesGetTheirTotalAndTheStockedQuantity()
    {
        var order = await _fixture.MakePurchaseOrderAsync(Ct);

        var line = await AddLineAsync(order.Id, 4, 2.5m);

        line.LineTotal.ShouldBe(10m);
        line.StockedQty.ShouldBe(3m);
    }

    [Fact]
    public async Task TheOrderTotalFollowsItsLinesTaxAndFreight()
    {
        var order = await _fixture.MakePurchaseOrderAsync(Ct);
        await AddLineAsync(order.Id, 2, 10m);
        var second = await AddLineAsync(order.Id, 3, 10m);

        var withLines = await OrderAsync(order.Id);
        await _fixture.Services.GetRequiredService<DeletePurchaseOrderDetail.Handler>().HandleAsync(new DeletePurchaseOrderDetail.Command(second.Id), Ct);
        var afterDelete = await OrderAsync(order.Id);

        withLines.SubTotal.ShouldBe(50m);
        withLines.TotalDue.ShouldBe(50m + order.TaxAmt + order.Freight);
        afterDelete.SubTotal.ShouldBe(20m);
        afterDelete.TotalDue.ShouldBe(20m + order.TaxAmt + order.Freight);
    }

    [Fact]
    public async Task ChangingTaxOrFreightUpdatesTheTotalDue()
    {
        var order = await _fixture.MakePurchaseOrderAsync(Ct);
        await AddLineAsync(order.Id, 1, 100m);

        var updated = await _fixture.Services.GetRequiredService<UpdatePurchaseOrder.Handler>().HandleAsync(
            new UpdatePurchaseOrder.Command(order.Id, order.Number, order.Status, order.BuyerId, order.VendorId, order.ShipMethodId, order.OrderDate, order.ShipDate, 25m, 10m), Ct);

        updated.Value.SubTotal.ShouldBe(100m);
        updated.Value.TotalDue.ShouldBe(135m);
    }

    [Fact]
    public async Task NumbersAreHandedOutInSequenceWhenLeftBlank()
    {
        var command = await _fixture.PurchaseOrderCommandAsync(Ct);
        var handler = _fixture.Services.GetRequiredService<CreatePurchaseOrder.Handler>();

        var first = await handler.HandleAsync(command with { Number = null }, Ct);
        var second = await handler.HandleAsync(command with { Number = null }, Ct);

        first.Value.Number.ShouldBe("PO-000001");
        second.Value.Number.ShouldBe("PO-000002");
    }

    [Fact]
    public async Task ADeletedProductRemovesItsVendorPrices()
    {
        var price = await _fixture.MakeProductVendorAsync(Ct);

        await _fixture.Services.GetRequiredService<IIntegrationEventConsumer<ProductDeleted>>().ConsumeAsync(new ProductDeleted(price.ProductId), Ct);
        var remaining = await _fixture.Services.GetRequiredService<ListProductVendors.Handler>().HandleAsync(new ListProductVendors.Query(), Ct);

        remaining.Items.ShouldBeEmpty();
    }
}

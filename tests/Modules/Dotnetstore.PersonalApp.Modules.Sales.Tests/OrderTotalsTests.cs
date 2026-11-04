using Dotnetstore.PersonalApp.Modules.Sales.Features.Customers;
using Dotnetstore.PersonalApp.Modules.Sales.Features.SalesOrderDetails;
using Dotnetstore.PersonalApp.Modules.Sales.Features.SalesOrders;
using Dotnetstore.PersonalApp.Modules.Sales.Features.SpecialOfferProducts;
using Dotnetstore.PersonalApp.Shared.Contracts.Events;
using Dotnetstore.PersonalApp.Shared.Events;
using Microsoft.Extensions.DependencyInjection;

namespace Dotnetstore.PersonalApp.Modules.Sales.Tests;

public sealed class OrderTotalsTests : IDisposable
{
    private readonly SalesFixture _fixture = new();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public void Dispose() => _fixture.Dispose();

    private async Task<SalesOrderDto> OrderAsync(Guid id)
    {
        var all = await _fixture.Services.GetRequiredService<ListSalesOrders.Handler>().HandleAsync(new ListSalesOrders.Query(), Ct);
        return all.Items.Single(o => o.Id == id);
    }

    private async Task<SalesOrderDetailDto> AddLineAsync(Guid orderId, int quantity, decimal unitPrice, decimal discount)
    {
        var command = await _fixture.SalesOrderDetailCommandAsync(Ct);
        var result = await _fixture.Services.GetRequiredService<CreateSalesOrderDetail.Handler>()
            .HandleAsync(command with { SalesOrderId = orderId, OrderQty = quantity, UnitPrice = unitPrice, UnitPriceDiscount = discount }, Ct);
        return result.Value;
    }

    [Fact]
    public async Task ALineTotalIsQuantityTimesPriceLessTheDiscount()
    {
        var order = await _fixture.MakeSalesOrderAsync(Ct);

        var line = await AddLineAsync(order.Id, 2, 10m, 0.25m);

        line.LineTotal.ShouldBe(15m);
    }

    [Fact]
    public async Task TheOrderTotalFollowsItsLinesTaxAndFreight()
    {
        var order = await _fixture.MakeSalesOrderAsync(Ct);
        await AddLineAsync(order.Id, 2, 10m, 0m);
        var second = await AddLineAsync(order.Id, 1, 40m, 0.5m);

        var withLines = await OrderAsync(order.Id);
        await _fixture.Services.GetRequiredService<DeleteSalesOrderDetail.Handler>().HandleAsync(new DeleteSalesOrderDetail.Command(second.Id), Ct);
        var afterDelete = await OrderAsync(order.Id);

        withLines.SubTotal.ShouldBe(40m);
        withLines.TotalDue.ShouldBe(40m + order.TaxAmt + order.Freight);
        afterDelete.SubTotal.ShouldBe(20m);
    }

    [Fact]
    public async Task DeletingAnOrderDeletesItsLines()
    {
        var order = await _fixture.MakeSalesOrderAsync(Ct);
        await AddLineAsync(order.Id, 1, 10m, 0m);

        var deleted = await _fixture.Services.GetRequiredService<DeleteSalesOrder.Handler>().HandleAsync(new DeleteSalesOrder.Command(order.Id), Ct);
        var lines = await _fixture.Services.GetRequiredService<ListSalesOrderDetails.Handler>().HandleAsync(new ListSalesOrderDetails.Query(), Ct);

        deleted.IsSuccess.ShouldBeTrue();
        lines.Items.ShouldBeEmpty();
    }

    [Fact]
    public async Task NumbersAreHandedOutInSequenceWhenLeftBlank()
    {
        var command = await _fixture.SalesOrderCommandAsync(Ct);
        var handler = _fixture.Services.GetRequiredService<CreateSalesOrder.Handler>();

        var first = await handler.HandleAsync(command with { Number = null }, Ct);
        var second = await handler.HandleAsync(command with { Number = null }, Ct);

        first.Value.Number.ShouldBe("SO-000001");
        second.Value.Number.ShouldBe("SO-000002");
    }

    [Fact]
    public async Task ADeletedProductRemovesItsOfferLinks()
    {
        var link = await _fixture.MakeSpecialOfferProductAsync(Ct);

        await _fixture.Services.GetRequiredService<IIntegrationEventConsumer<ProductDeleted>>().ConsumeAsync(new ProductDeleted(link.ProductId), Ct);
        var remaining = await _fixture.Services.GetRequiredService<ListSpecialOfferProducts.Handler>().HandleAsync(new ListSpecialOfferProducts.Query(), Ct);

        remaining.Items.ShouldBeEmpty();
    }

    [Fact]
    public async Task ADeletedClientIsUnlinkedFromItsCustomersWhichAreKept()
    {
        var clientId = Guid.NewGuid();
        var command = await _fixture.CustomerCommandAsync(Ct);
        await _fixture.Services.GetRequiredService<CreateCustomer.Handler>().HandleAsync(command with { ClientId = clientId }, Ct);

        await _fixture.Services.GetRequiredService<IIntegrationEventConsumer<ClientDeleted>>().ConsumeAsync(new ClientDeleted(clientId), Ct);
        var customers = await _fixture.Services.GetRequiredService<ListCustomers.Handler>().HandleAsync(new ListCustomers.Query(), Ct);

        customers.Items.ShouldHaveSingleItem().ClientId.ShouldBeNull();
    }
}

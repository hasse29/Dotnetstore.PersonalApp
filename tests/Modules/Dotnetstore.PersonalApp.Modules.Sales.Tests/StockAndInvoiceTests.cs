using Dotnetstore.PersonalApp.Modules.Sales.Domain;
using Dotnetstore.PersonalApp.Modules.Sales.Features.Invoices;
using Dotnetstore.PersonalApp.Modules.Sales.Features.Offers;
using Dotnetstore.PersonalApp.Modules.Sales.Features.Orders;
using Dotnetstore.PersonalApp.Modules.Sales.Features.PickLists;
using Dotnetstore.PersonalApp.Shared.Results;

namespace Dotnetstore.PersonalApp.Modules.Sales.Tests;

public sealed class StockAndInvoiceTests
{
    private sealed record Setup(OrderDetailDto Order, Guid PickListId, IReadOnlyList<PickListLineDto> Lines, Guid ProductId, Guid LocationA, Guid LocationB);

    private static async Task<Setup> OrderWithStockAsync(SalesFixture fixture, decimal quantity, decimal inA, decimal inB)
    {
        var productId = Guid.NewGuid();
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        fixture.Stock.Add(productId, a, inA);
        fixture.Stock.Add(productId, b, inB);

        var customer = await fixture.NewCustomerAsync(Guid.NewGuid().ToString("N"));
        var offer = await fixture.NewOfferAsync(customer.Id, new OfferLineFields(productId, "Cable", quantity, "Piece", 100, 25, 0, IsPhysical: true));
        await fixture.ChangeStatus.HandleAsync(new ChangeOfferStatus.Command(offer.Offer.Id, OfferStatus.Sent), CancellationToken.None);
        await fixture.ChangeStatus.HandleAsync(new ChangeOfferStatus.Command(offer.Offer.Id, OfferStatus.Accepted), CancellationToken.None);
        var order = (await fixture.CreateOrder.HandleAsync(new CreateOrderFromOffer.Command(offer.Offer.Id), CancellationToken.None)).Value;
        var pickList = (await fixture.ListPickLists.HandleAsync(new ListPickLists.Query(), CancellationToken.None)).Items
            .Single(p => p.OrderId == order.Order.Id);
        var lines = (await fixture.GetPickList.HandleAsync(new GetPickList.Query(pickList.Id), CancellationToken.None)).Value.Lines;
        return new Setup(order, pickList.Id, lines, productId, a, b);
    }

    private static Task<Result<PickListDetailDto>> PickAsync(SalesFixture fixture, Setup setup, decimal quantity) =>
        fixture.SavePicked.HandleAsync(new SavePickedQuantities.Command(setup.PickListId, [new(setup.Lines[0].Id, quantity)]), CancellationToken.None);

    private static async Task<OrderDetailDto> ReadyOrderAsync(SalesFixture fixture)
    {
        var setup = await OrderWithStockAsync(fixture, 4, 10, 0);
        await PickAsync(fixture, setup, 4);
        return (await fixture.GetOrder.HandleAsync(new GetOrder.Query(setup.Order.Order.Id), CancellationToken.None)).Value;
    }

    [Fact]
    public async Task PickingIssuesStockFromTheLocationsWithTheMostFirst()
    {
        using var fixture = new SalesFixture();
        var setup = await OrderWithStockAsync(fixture, 10, 6, 8);

        var result = await PickAsync(fixture, setup, 10);

        result.IsSuccess.ShouldBeTrue();
        fixture.Stock.At(setup.ProductId, setup.LocationB).ShouldBe(0);
        fixture.Stock.At(setup.ProductId, setup.LocationA).ShouldBe(4);
    }

    [Fact]
    public async Task PickingLessPutsTheGoodsBackWhereTheyCameFrom()
    {
        using var fixture = new SalesFixture();
        var setup = await OrderWithStockAsync(fixture, 10, 6, 8);
        await PickAsync(fixture, setup, 10);

        // Which of the two locations gets the goods first is not specified, only that none gets more than it gave.
        await PickAsync(fixture, setup, 7);
        fixture.Stock.Total(setup.ProductId).ShouldBe(7);
        fixture.Stock.At(setup.ProductId, setup.LocationA).ShouldBeLessThanOrEqualTo(6);
        fixture.Stock.At(setup.ProductId, setup.LocationB).ShouldBeLessThanOrEqualTo(8);

        await PickAsync(fixture, setup, 0);
        fixture.Stock.At(setup.ProductId, setup.LocationA).ShouldBe(6);
        fixture.Stock.At(setup.ProductId, setup.LocationB).ShouldBe(8);
    }

    [Fact]
    public async Task NotEnoughStockFailsAndLeavesWhatWasPickedBefore()
    {
        using var fixture = new SalesFixture();
        var setup = await OrderWithStockAsync(fixture, 10, 3, 3);

        var first = await PickAsync(fixture, setup, 5);
        var tooMuch = await PickAsync(fixture, setup, 10);

        first.IsSuccess.ShouldBeTrue();
        tooMuch.Error.Code.ShouldBe("stock_insufficient_total");
        fixture.Stock.Total(setup.ProductId).ShouldBe(1);
        var line = (await fixture.GetPickList.HandleAsync(new GetPickList.Query(setup.PickListId), CancellationToken.None)).Value.Lines.Single();
        line.PickedQuantity.ShouldBe(5);
    }

    [Fact]
    public async Task CancellingTheOrderReturnsEverythingPicked()
    {
        using var fixture = new SalesFixture();
        var setup = await OrderWithStockAsync(fixture, 10, 6, 8);
        await PickAsync(fixture, setup, 10);

        var cancelled = await fixture.CancelOrder.HandleAsync(new CancelOrder.Command(setup.Order.Order.Id), CancellationToken.None);

        cancelled.IsSuccess.ShouldBeTrue();
        fixture.Stock.At(setup.ProductId, setup.LocationA).ShouldBe(6);
        fixture.Stock.At(setup.ProductId, setup.LocationB).ShouldBe(8);
    }

    [Fact]
    public async Task CreatesAnInvoiceFromAReadyOrder()
    {
        using var fixture = new SalesFixture();
        var order = await ReadyOrderAsync(fixture);

        var result = await fixture.CreateInvoice.HandleAsync(new CreateInvoiceFromOrder.Command(order.Order.Id, 14), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        var invoice = result.Value;
        invoice.Invoice.Number.ShouldBe("INV-2026-0001");
        invoice.Invoice.Status.ShouldBe(InvoiceStatus.Unpaid);
        invoice.Invoice.Date.ShouldBe(new DateOnly(2026, 11, 10));
        invoice.Invoice.DueDate.ShouldBe(new DateOnly(2026, 11, 24));
        invoice.Totals.Gross.ShouldBe(order.Totals.Gross);
        invoice.Lines.Count.ShouldBe(1);
        (await fixture.GetOrder.HandleAsync(new GetOrder.Query(order.Order.Id), CancellationToken.None)).Value.Order.Status.ShouldBe(OrderStatus.Invoiced);
    }

    [Fact]
    public async Task OnlyAReadyOrderCanBeInvoicedAndOnlyOnce()
    {
        using var fixture = new SalesFixture();
        var notPicked = await OrderWithStockAsync(fixture, 4, 10, 0);
        var ready = await ReadyOrderAsync(fixture);

        var early = await fixture.CreateInvoice.HandleAsync(new CreateInvoiceFromOrder.Command(notPicked.Order.Order.Id), CancellationToken.None);
        await fixture.CreateInvoice.HandleAsync(new CreateInvoiceFromOrder.Command(ready.Order.Id), CancellationToken.None);
        var twice = await fixture.CreateInvoice.HandleAsync(new CreateInvoiceFromOrder.Command(ready.Order.Id), CancellationToken.None);
        var invalid = await fixture.CreateInvoice.HandleAsync(new CreateInvoiceFromOrder.Command(ready.Order.Id, 400), CancellationToken.None);

        early.Error.Code.ShouldBe("invoice_order_not_ready");
        twice.Error.Code.ShouldBe("invoice_order_not_ready");
        invalid.Error.Code.ShouldBe("invoice_payment_days_invalid");
    }

    [Fact]
    public async Task AnInvoicedOrderLocksThePickListAndCannotBeCancelled()
    {
        using var fixture = new SalesFixture();
        var setup = await OrderWithStockAsync(fixture, 4, 10, 0);
        await PickAsync(fixture, setup, 4);
        await fixture.CreateInvoice.HandleAsync(new CreateInvoiceFromOrder.Command(setup.Order.Order.Id), CancellationToken.None);

        var pick = await PickAsync(fixture, setup, 1);
        var cancel = await fixture.CancelOrder.HandleAsync(new CancelOrder.Command(setup.Order.Order.Id), CancellationToken.None);

        pick.Error.Code.ShouldBe("picklist_locked");
        cancel.Error.Code.ShouldBe("order_invoiced");
        fixture.Stock.Total(setup.ProductId).ShouldBe(6);
    }

    [Fact]
    public async Task CancellingAnInvoiceReopensTheOrderAndTheNumberIsNotReused()
    {
        using var fixture = new SalesFixture();
        var order = await ReadyOrderAsync(fixture);
        var first = (await fixture.CreateInvoice.HandleAsync(new CreateInvoiceFromOrder.Command(order.Order.Id), CancellationToken.None)).Value;

        var cancelled = await fixture.CancelInvoice.HandleAsync(new CancelInvoice.Command(first.Invoice.Id), CancellationToken.None);
        var reopened = (await fixture.GetOrder.HandleAsync(new GetOrder.Query(order.Order.Id), CancellationToken.None)).Value;
        var second = await fixture.CreateInvoice.HandleAsync(new CreateInvoiceFromOrder.Command(order.Order.Id), CancellationToken.None);

        cancelled.Value.Invoice.Status.ShouldBe(InvoiceStatus.Cancelled);
        reopened.Order.Status.ShouldBe(OrderStatus.ReadyToInvoice);
        reopened.InvoiceNumber.ShouldBeNull();
        second.Value.Invoice.Number.ShouldBe("INV-2026-0002");
    }

    [Fact]
    public async Task ListsInvoicesNewestFirstAndFiltersByStatus()
    {
        using var fixture = new SalesFixture();
        var one = await ReadyOrderAsync(fixture);
        var two = await ReadyOrderAsync(fixture);
        var invoiceOne = (await fixture.CreateInvoice.HandleAsync(new CreateInvoiceFromOrder.Command(one.Order.Id), CancellationToken.None)).Value;
        await fixture.CreateInvoice.HandleAsync(new CreateInvoiceFromOrder.Command(two.Order.Id), CancellationToken.None);
        await fixture.RecordPayment.HandleAsync(new RecordPayment.Command(invoiceOne.Invoice.Id, invoiceOne.Totals.Gross), CancellationToken.None);

        var all = await fixture.ListInvoices.HandleAsync(new ListInvoices.Query(), CancellationToken.None);
        var unpaid = await fixture.ListInvoices.HandleAsync(new ListInvoices.Query(Status: InvoiceStatus.Unpaid), CancellationToken.None);

        all.Items.Select(i => i.Number).ShouldBe(["INV-2026-0002", "INV-2026-0001"]);
        unpaid.Items.Single().Number.ShouldBe("INV-2026-0002");
    }
}

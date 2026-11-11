using Dotnetstore.PersonalApp.Modules.Sales.Domain;
using Dotnetstore.PersonalApp.Modules.Sales.Features.Offers;
using Dotnetstore.PersonalApp.Modules.Sales.Features.Orders;
using Dotnetstore.PersonalApp.Modules.Sales.Features.PickLists;

namespace Dotnetstore.PersonalApp.Modules.Sales.Tests;

public sealed class OrderTests
{
    private static OfferLineFields Goods(SalesFixture fixture, string description, decimal quantity, decimal inStock = 1000)
    {
        var productId = Guid.NewGuid();
        fixture.Stock.Add(productId, Guid.NewGuid(), inStock);
        return new(productId, description, quantity, "Piece", 100, 25, 0, IsPhysical: true);
    }

    private static async Task<OfferDetailDto> AcceptedOfferAsync(SalesFixture fixture, params OfferLineFields[] lines)
    {
        var customer = await fixture.NewCustomerAsync(Guid.NewGuid().ToString("N"));
        var offer = await fixture.NewOfferAsync(customer.Id, lines);
        await fixture.ChangeStatus.HandleAsync(new ChangeOfferStatus.Command(offer.Offer.Id, OfferStatus.Sent), CancellationToken.None);
        await fixture.ChangeStatus.HandleAsync(new ChangeOfferStatus.Command(offer.Offer.Id, OfferStatus.Accepted), CancellationToken.None);
        return offer;
    }

    [Fact]
    public async Task OnlyAnAcceptedOfferCanBeOrdered()
    {
        using var fixture = new SalesFixture();
        var customer = await fixture.NewCustomerAsync();
        var draft = await fixture.NewOfferAsync(customer.Id, Goods(fixture, "Cable", 2));

        var result = await fixture.CreateOrder.HandleAsync(new CreateOrderFromOffer.Command(draft.Offer.Id), CancellationToken.None);

        result.Error.Code.ShouldBe("order_offer_not_accepted");
    }

    [Fact]
    public async Task CreatesAnOrderWithCopiedLinesAndAPickListForTheGoods()
    {
        using var fixture = new SalesFixture();
        var offer = await AcceptedOfferAsync(fixture, Goods(fixture, "Cable", 10), SalesFixture.Line("Consulting"), Goods(fixture, "Switch", 2));

        var result = await fixture.CreateOrder.HandleAsync(new CreateOrderFromOffer.Command(offer.Offer.Id), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        var order = result.Value;
        order.Order.Number.ShouldBe("ORD-2026-0001");
        order.Order.OfferNumber.ShouldBe(offer.Offer.Number);
        order.Order.Status.ShouldBe(OrderStatus.Open);
        order.Lines.Count.ShouldBe(3);
        order.Totals.Gross.ShouldBe(offer.Totals.Gross);
        order.PickListNumber.ShouldBe("PL-2026-0001");

        var picks = await fixture.ListPickLists.HandleAsync(new ListPickLists.Query(), CancellationToken.None);
        var detail = await fixture.GetPickList.HandleAsync(new GetPickList.Query(picks.Items.Single().Id), CancellationToken.None);
        detail.Value.Lines.Select(l => l.Description).ShouldBe(["Cable", "Switch"]);
    }

    [Fact]
    public async Task AnOrderWithNothingToPickIsReadyToInvoiceAtOnce()
    {
        using var fixture = new SalesFixture();
        var offer = await AcceptedOfferAsync(fixture, SalesFixture.Line("Consulting"));

        var result = await fixture.CreateOrder.HandleAsync(new CreateOrderFromOffer.Command(offer.Offer.Id), CancellationToken.None);

        result.Value.Order.Status.ShouldBe(OrderStatus.ReadyToInvoice);
        result.Value.PickListNumber.ShouldBeNull();
        (await fixture.ListPickLists.HandleAsync(new ListPickLists.Query(), CancellationToken.None)).TotalCount.ShouldBe(0);
    }

    [Fact]
    public async Task AnOfferGetsOneLiveOrderAtATime()
    {
        using var fixture = new SalesFixture();
        var offer = await AcceptedOfferAsync(fixture, Goods(fixture, "Cable", 1));
        var first = (await fixture.CreateOrder.HandleAsync(new CreateOrderFromOffer.Command(offer.Offer.Id), CancellationToken.None)).Value;

        var again = await fixture.CreateOrder.HandleAsync(new CreateOrderFromOffer.Command(offer.Offer.Id), CancellationToken.None);
        (await fixture.GetOffer.HandleAsync(new GetOffer.Query(offer.Offer.Id), CancellationToken.None)).Value.OrderNumber.ShouldBe(first.Order.Number);
        await fixture.CancelOrder.HandleAsync(new CancelOrder.Command(first.Order.Id), CancellationToken.None);
        var afterCancel = await fixture.CreateOrder.HandleAsync(new CreateOrderFromOffer.Command(offer.Offer.Id), CancellationToken.None);

        again.Error.Code.ShouldBe("order_already_exists");
        afterCancel.IsSuccess.ShouldBeTrue();
        afterCancel.Value.Order.Number.ShouldBe("ORD-2026-0002");
    }

    [Fact]
    public async Task PickingEverythingCompletesTheListAndReadiesTheOrder()
    {
        using var fixture = new SalesFixture();
        var offer = await AcceptedOfferAsync(fixture, Goods(fixture, "Cable", 10), Goods(fixture, "Switch", 2));
        var order = (await fixture.CreateOrder.HandleAsync(new CreateOrderFromOffer.Command(offer.Offer.Id), CancellationToken.None)).Value;
        var pickList = (await fixture.ListPickLists.HandleAsync(new ListPickLists.Query(), CancellationToken.None)).Items.Single();
        var lines = (await fixture.GetPickList.HandleAsync(new GetPickList.Query(pickList.Id), CancellationToken.None)).Value.Lines;

        var partial = await fixture.SavePicked.HandleAsync(
            new SavePickedQuantities.Command(pickList.Id, [new(lines[0].Id, 10), new(lines[1].Id, 1)]), CancellationToken.None);
        partial.Value.PickList.Status.ShouldBe(PickListStatus.Open);
        partial.Value.PickList.PickedLineCount.ShouldBe(1);
        (await fixture.GetOrder.HandleAsync(new GetOrder.Query(order.Order.Id), CancellationToken.None)).Value.Order.Status.ShouldBe(OrderStatus.Open);

        var done = await fixture.SavePicked.HandleAsync(
            new SavePickedQuantities.Command(pickList.Id, [new(lines[1].Id, 2)]), CancellationToken.None);
        done.Value.PickList.Status.ShouldBe(PickListStatus.Complete);
        (await fixture.GetOrder.HandleAsync(new GetOrder.Query(order.Order.Id), CancellationToken.None)).Value.Order.Status.ShouldBe(OrderStatus.ReadyToInvoice);

        var reopened = await fixture.SavePicked.HandleAsync(
            new SavePickedQuantities.Command(pickList.Id, [new(lines[1].Id, 1)]), CancellationToken.None);
        reopened.Value.PickList.Status.ShouldBe(PickListStatus.Open);
        (await fixture.GetOrder.HandleAsync(new GetOrder.Query(order.Order.Id), CancellationToken.None)).Value.Order.Status.ShouldBe(OrderStatus.Open);
    }

    [Fact]
    public async Task RejectsPickingMoreThanOrderedOrLessThanNothing()
    {
        using var fixture = new SalesFixture();
        var offer = await AcceptedOfferAsync(fixture, Goods(fixture, "Cable", 10));
        await fixture.CreateOrder.HandleAsync(new CreateOrderFromOffer.Command(offer.Offer.Id), CancellationToken.None);
        var pickList = (await fixture.ListPickLists.HandleAsync(new ListPickLists.Query(), CancellationToken.None)).Items.Single();
        var line = (await fixture.GetPickList.HandleAsync(new GetPickList.Query(pickList.Id), CancellationToken.None)).Value.Lines.Single();

        var tooMany = await fixture.SavePicked.HandleAsync(new SavePickedQuantities.Command(pickList.Id, [new(line.Id, 11)]), CancellationToken.None);
        var negative = await fixture.SavePicked.HandleAsync(new SavePickedQuantities.Command(pickList.Id, [new(line.Id, -1)]), CancellationToken.None);

        tooMany.Error.Code.ShouldBe("picklist_picked_invalid");
        negative.Error.Code.ShouldBe("picklist_picked_invalid");
    }

    [Fact]
    public async Task CancellingTheOrderCancelsThePickListAndLocksIt()
    {
        using var fixture = new SalesFixture();
        var offer = await AcceptedOfferAsync(fixture, Goods(fixture, "Cable", 10));
        var order = (await fixture.CreateOrder.HandleAsync(new CreateOrderFromOffer.Command(offer.Offer.Id), CancellationToken.None)).Value;
        var pickList = (await fixture.ListPickLists.HandleAsync(new ListPickLists.Query(), CancellationToken.None)).Items.Single();
        var line = (await fixture.GetPickList.HandleAsync(new GetPickList.Query(pickList.Id), CancellationToken.None)).Value.Lines.Single();

        var cancelled = await fixture.CancelOrder.HandleAsync(new CancelOrder.Command(order.Order.Id), CancellationToken.None);
        var pick = await fixture.SavePicked.HandleAsync(new SavePickedQuantities.Command(pickList.Id, [new(line.Id, 10)]), CancellationToken.None);
        var cancelAgain = await fixture.CancelOrder.HandleAsync(new CancelOrder.Command(order.Order.Id), CancellationToken.None);

        cancelled.Value.Order.Status.ShouldBe(OrderStatus.Cancelled);
        (await fixture.GetPickList.HandleAsync(new GetPickList.Query(pickList.Id), CancellationToken.None)).Value.PickList.Status.ShouldBe(PickListStatus.Cancelled);
        pick.Error.Code.ShouldBe("picklist_cancelled");
        cancelAgain.Error.Code.ShouldBe("order_already_cancelled");
    }

    [Fact]
    public async Task ListsOrdersNewestFirstAndFiltersByStatus()
    {
        using var fixture = new SalesFixture();
        var first = await AcceptedOfferAsync(fixture, SalesFixture.Line("Consulting"));
        var second = await AcceptedOfferAsync(fixture, Goods(fixture, "Cable", 1));
        await fixture.CreateOrder.HandleAsync(new CreateOrderFromOffer.Command(first.Offer.Id), CancellationToken.None);
        await fixture.CreateOrder.HandleAsync(new CreateOrderFromOffer.Command(second.Offer.Id), CancellationToken.None);

        var all = await fixture.ListOrders.HandleAsync(new ListOrders.Query(), CancellationToken.None);
        var open = await fixture.ListOrders.HandleAsync(new ListOrders.Query(Status: OrderStatus.Open), CancellationToken.None);

        all.Items.Select(o => o.Number).ShouldBe(["ORD-2026-0002", "ORD-2026-0001"]);
        open.Items.Single().Number.ShouldBe("ORD-2026-0002");
        open.Items.Single().PickLines.ShouldBe(1);
    }
}

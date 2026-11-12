using Dotnetstore.PersonalApp.Modules.Sales.Domain;
using Dotnetstore.PersonalApp.Modules.Sales.Features.Offers;

namespace Dotnetstore.PersonalApp.Modules.Sales.Tests;

public sealed class OfferTests
{
    [Fact]
    public async Task CreatesADraftWithTheNextNumberAndACopyOfTheCustomer()
    {
        using var fixture = new SalesFixture();
        var customer = await fixture.NewCustomerAsync();
        fixture.Contacts.SetAddress(customer.Id, "Main Street 1, 111 22 Stockholm");

        var first = await fixture.NewOfferAsync(customer.Id, SalesFixture.Line());
        var second = await fixture.NewOfferAsync(customer.Id, SalesFixture.Line());

        first.Offer.Number.ShouldBe("O-2026-0001");
        second.Offer.Number.ShouldBe("O-2026-0002");
        first.Offer.Status.ShouldBe(OfferStatus.Draft);
        first.Offer.CustomerName.ShouldBe("Acme School");
        first.CustomerAddress.ShouldBe("Main Street 1\n111 22 Stockholm");
    }

    [Fact]
    public async Task DoesNotReuseTheNumberOfADeletedOffer()
    {
        using var fixture = new SalesFixture();
        var customer = await fixture.NewCustomerAsync();
        await fixture.NewOfferAsync(customer.Id, SalesFixture.Line());
        var second = await fixture.NewOfferAsync(customer.Id, SalesFixture.Line());
        await fixture.DeleteOffer.HandleAsync(new DeleteOffer.Command(second.Offer.Id), CancellationToken.None);

        var third = await fixture.NewOfferAsync(customer.Id, SalesFixture.Line());

        third.Offer.Number.ShouldBe("O-2026-0002");
    }

    [Fact]
    public async Task AddsUpLinesWithDiscountAndVatPerLine()
    {
        using var fixture = new SalesFixture();
        var customer = await fixture.NewCustomerAsync();

        // 10 x 50 = 500 with 10 % off = 450, VAT 25 % = 112.50; 2 x 100 = 200, VAT 6 % = 12.
        var offer = await fixture.NewOfferAsync(
            customer.Id,
            SalesFixture.Line(quantity: 10, price: 50, vat: 25, discount: 10),
            SalesFixture.Line("Book", quantity: 2, price: 100, vat: 6));

        offer.Totals.Net.ShouldBe(650m);
        offer.Totals.Vat.ShouldBe(124.50m);
        offer.Totals.Gross.ShouldBe(774.50m);
        offer.Offer.Total.ShouldBe(774.50m);
    }

    [Fact]
    public async Task RejectsAnOfferToAnUnknownCustomer()
    {
        using var fixture = new SalesFixture();

        var result = await fixture.CreateOffer.HandleAsync(
            new CreateOffer.Command(SalesFixture.Offer(Guid.NewGuid(), SalesFixture.Line())), CancellationToken.None);

        result.Error.Code.ShouldBe("offer_customer_not_found");
    }

    [Fact]
    public async Task RejectsAnInactiveCustomer()
    {
        using var fixture = new SalesFixture();
        var customer = await fixture.CreateCustomer.HandleAsync(
            new Dotnetstore.PersonalApp.Modules.Sales.Features.Customers.CreateCustomer.Command(SalesFixture.Customer() with { IsActive = false }),
            CancellationToken.None);

        var result = await fixture.CreateOffer.HandleAsync(
            new CreateOffer.Command(SalesFixture.Offer(customer.Value.Id, SalesFixture.Line())), CancellationToken.None);

        result.Error.Code.ShouldBe("offer_customer_inactive");
    }

    [Fact]
    public async Task RejectsInvalidLinesAndDates()
    {
        using var fixture = new SalesFixture();
        var customer = await fixture.NewCustomerAsync();

        var noQuantity = await fixture.CreateOffer.HandleAsync(
            new CreateOffer.Command(SalesFixture.Offer(customer.Id, SalesFixture.Line(quantity: 0))), CancellationToken.None);
        var badDiscount = await fixture.CreateOffer.HandleAsync(
            new CreateOffer.Command(SalesFixture.Offer(customer.Id, SalesFixture.Line(discount: 101))), CancellationToken.None);
        var badDates = await fixture.CreateOffer.HandleAsync(
            new CreateOffer.Command(SalesFixture.Offer(customer.Id, SalesFixture.Line()) with { ValidUntil = new DateOnly(2026, 11, 1) }),
            CancellationToken.None);

        noQuantity.Error.Code.ShouldBe("offer_line_quantity_invalid");
        badDiscount.Error.Code.ShouldBe("offer_line_discount_invalid");
        badDates.Error.Code.ShouldBe("offer_valid_until_before_date");
    }

    [Fact]
    public async Task ReplacesTheLinesWhenADraftIsUpdated()
    {
        using var fixture = new SalesFixture();
        var customer = await fixture.NewCustomerAsync();
        var offer = await fixture.NewOfferAsync(customer.Id, SalesFixture.Line(), SalesFixture.Line("Extra"));

        var result = await fixture.UpdateOffer.HandleAsync(
            new UpdateOffer.Command(offer.Offer.Id, SalesFixture.Offer(customer.Id, SalesFixture.Line("Only one", quantity: 1, price: 10, vat: 0))),
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Lines.Count.ShouldBe(1);
        result.Value.Lines[0].Description.ShouldBe("Only one");
        (await fixture.GetOffer.HandleAsync(new GetOffer.Query(offer.Offer.Id), CancellationToken.None)).Value.Lines.Count.ShouldBe(1);
    }

    [Fact]
    public async Task CannotSendAnOfferWithoutLines()
    {
        using var fixture = new SalesFixture();
        var customer = await fixture.NewCustomerAsync();
        var offer = await fixture.NewOfferAsync(customer.Id);

        var result = await fixture.ChangeStatus.HandleAsync(new ChangeOfferStatus.Command(offer.Offer.Id, OfferStatus.Sent), CancellationToken.None);

        result.Error.Code.ShouldBe("offer_no_lines");
    }

    [Fact]
    public async Task FollowsTheStatusFlowAndLocksAfterSending()
    {
        using var fixture = new SalesFixture();
        var customer = await fixture.NewCustomerAsync();
        var offer = await fixture.NewOfferAsync(customer.Id, SalesFixture.Line());
        var id = offer.Offer.Id;

        (await fixture.ChangeStatus.HandleAsync(new ChangeOfferStatus.Command(id, OfferStatus.Accepted), CancellationToken.None))
            .Error.Code.ShouldBe("offer_status_invalid");

        (await fixture.ChangeStatus.HandleAsync(new ChangeOfferStatus.Command(id, OfferStatus.Sent), CancellationToken.None))
            .IsSuccess.ShouldBeTrue();

        var edit = await fixture.UpdateOffer.HandleAsync(
            new UpdateOffer.Command(id, SalesFixture.Offer(customer.Id, SalesFixture.Line())), CancellationToken.None);
        edit.Error.Code.ShouldBe("offer_not_editable");

        var accepted = await fixture.ChangeStatus.HandleAsync(new ChangeOfferStatus.Command(id, OfferStatus.Accepted), CancellationToken.None);
        accepted.Value.Offer.Status.ShouldBe(OfferStatus.Accepted);

        (await fixture.ChangeStatus.HandleAsync(new ChangeOfferStatus.Command(id, OfferStatus.Draft), CancellationToken.None))
            .Error.Code.ShouldBe("offer_status_invalid");
        (await fixture.DeleteOffer.HandleAsync(new DeleteOffer.Command(id), CancellationToken.None))
            .Error.Code.ShouldBe("offer_not_deletable");
    }

    [Fact]
    public async Task ADeclinedOfferCanBeReopenedOrDeleted()
    {
        using var fixture = new SalesFixture();
        var customer = await fixture.NewCustomerAsync();
        var offer = await fixture.NewOfferAsync(customer.Id, SalesFixture.Line());
        var id = offer.Offer.Id;
        await fixture.ChangeStatus.HandleAsync(new ChangeOfferStatus.Command(id, OfferStatus.Sent), CancellationToken.None);
        await fixture.ChangeStatus.HandleAsync(new ChangeOfferStatus.Command(id, OfferStatus.Declined), CancellationToken.None);

        var reopened = await fixture.ChangeStatus.HandleAsync(new ChangeOfferStatus.Command(id, OfferStatus.Draft), CancellationToken.None);
        await fixture.ChangeStatus.HandleAsync(new ChangeOfferStatus.Command(id, OfferStatus.Sent), CancellationToken.None);
        await fixture.ChangeStatus.HandleAsync(new ChangeOfferStatus.Command(id, OfferStatus.Declined), CancellationToken.None);
        var deleted = await fixture.DeleteOffer.HandleAsync(new DeleteOffer.Command(id), CancellationToken.None);

        reopened.Value.Offer.Status.ShouldBe(OfferStatus.Draft);
        deleted.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task ASentOfferPastItsValidUntilDateIsShownAsExpired()
    {
        using var fixture = new SalesFixture();
        var customer = await fixture.NewCustomerAsync();
        var offer = await fixture.CreateOffer.HandleAsync(
            new CreateOffer.Command(SalesFixture.Offer(customer.Id, SalesFixture.Line()) with
            {
                Date = new DateOnly(2026, 10, 1),
                ValidUntil = new DateOnly(2026, 10, 31),
            }),
            CancellationToken.None);
        await fixture.ChangeStatus.HandleAsync(new ChangeOfferStatus.Command(offer.Value.Offer.Id, OfferStatus.Sent), CancellationToken.None);

        var list = await fixture.ListOffers.HandleAsync(new ListOffers.Query(), CancellationToken.None);

        list.Items.Single().IsExpired.ShouldBeTrue();
        list.Items.Single().Status.ShouldBe(OfferStatus.Sent);
    }

    [Fact]
    public async Task ListsNewestFirstAndFiltersByStatusAndSearch()
    {
        using var fixture = new SalesFixture();
        var acme = await fixture.NewCustomerAsync("Acme School");
        var beta = await fixture.NewCustomerAsync("Beta Corp");
        await fixture.NewOfferAsync(acme.Id, SalesFixture.Line());
        var second = await fixture.NewOfferAsync(beta.Id, SalesFixture.Line());
        await fixture.ChangeStatus.HandleAsync(new ChangeOfferStatus.Command(second.Offer.Id, OfferStatus.Sent), CancellationToken.None);

        var all = await fixture.ListOffers.HandleAsync(new ListOffers.Query(), CancellationToken.None);
        var sent = await fixture.ListOffers.HandleAsync(new ListOffers.Query(Status: OfferStatus.Sent), CancellationToken.None);
        var searched = await fixture.ListOffers.HandleAsync(new ListOffers.Query("acme"), CancellationToken.None);

        all.Items.Select(o => o.Number).ShouldBe(["O-2026-0002", "O-2026-0001"]);
        sent.Items.Single().CustomerName.ShouldBe("Beta Corp");
        searched.Items.Single().CustomerName.ShouldBe("Acme School");
    }

    [Fact]
    public async Task KeepsTheCopiedCustomerNameWhenTheCustomerIsRenamed()
    {
        using var fixture = new SalesFixture();
        var customer = await fixture.NewCustomerAsync("Acme School");
        var offer = await fixture.NewOfferAsync(customer.Id, SalesFixture.Line());

        await fixture.UpdateCustomer.HandleAsync(
            new Dotnetstore.PersonalApp.Modules.Sales.Features.Customers.UpdateCustomer.Command(customer.Id, SalesFixture.Customer("Acme Academy")),
            CancellationToken.None);

        (await fixture.GetOffer.HandleAsync(new GetOffer.Query(offer.Offer.Id), CancellationToken.None)).Value.Offer.CustomerName.ShouldBe("Acme School");
    }

    [Fact]
    public async Task AnOfferForACustomerWithoutAnAddressHasNone()
    {
        using var fixture = new SalesFixture();
        var customer = await fixture.NewCustomerAsync();

        var offer = await fixture.NewOfferAsync(customer.Id, SalesFixture.Line());

        offer.CustomerAddress.ShouldBeNull();
    }

    [Fact]
    public async Task ADraftOfferPicksUpAnAddressAddedLater()
    {
        using var fixture = new SalesFixture();
        var customer = await fixture.NewCustomerAsync();
        var offer = await fixture.NewOfferAsync(customer.Id, SalesFixture.Line());
        fixture.Contacts.SetAddress(customer.Id, "Main Street 1, 111 22 Stockholm");

        var result = await fixture.UpdateOffer.HandleAsync(
            new UpdateOffer.Command(offer.Offer.Id, SalesFixture.Offer(customer.Id, SalesFixture.Line())), CancellationToken.None);

        result.Value.CustomerAddress.ShouldBe("Main Street 1\n111 22 Stockholm");
    }

    [Fact]
    public async Task ADraftKeepsItsAddressWhenTheCustomerNoLongerHasOne()
    {
        using var fixture = new SalesFixture();
        var customer = await fixture.NewCustomerAsync();
        fixture.Contacts.SetAddress(customer.Id, "Main Street 1, 111 22 Stockholm");
        var offer = await fixture.NewOfferAsync(customer.Id, SalesFixture.Line());
        fixture.Contacts.RemoveAddress(customer.Id);

        var result = await fixture.UpdateOffer.HandleAsync(
            new UpdateOffer.Command(offer.Offer.Id, SalesFixture.Offer(customer.Id, SalesFixture.Line("Another line"))), CancellationToken.None);

        result.Value.CustomerAddress.ShouldBe("Main Street 1\n111 22 Stockholm");
    }
}

using Dotnetstore.PersonalApp.Modules.Sales.Features;
using Dotnetstore.PersonalApp.Modules.Sales.Features.Offers;
using Dotnetstore.PersonalApp.Modules.Sales.Features.Orders;

namespace Dotnetstore.PersonalApp.Modules.Sales.Tests;

public sealed class DocumentNumberTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public void ThePrefixIsTheKindAndTheDate()
    {
        DocumentNumber.Prefix(DocumentNumber.Offer, new DateOnly(2026, 11, 12)).ShouldBe("OFF20261112");
        DocumentNumber.Prefix(DocumentNumber.Order, new DateOnly(2026, 1, 2)).ShouldBe("ORD20260102");
        DocumentNumber.Prefix(DocumentNumber.Invoice, new DateOnly(2026, 11, 12)).ShouldBe("INV20261112");
    }

    [Fact]
    public void TheFirstNumberOfTheDayEndsInOne() =>
        DocumentNumber.Next("OFF20261112", []).ShouldBe("OFF202611120001");

    [Fact]
    public void TheNextNumberIsOneMoreThanTheHighestEvenWithGaps() =>
        DocumentNumber.Next("OFF20261112", ["OFF202611120001", "OFF202611120007", "OFF202611120003"]).ShouldBe("OFF202611120008");

    [Fact]
    public void ANumberThatCannotBeReadCountsAsZero() =>
        DocumentNumber.Next("OFF20261112", ["OFF20261112abcd"]).ShouldBe("OFF202611120001");

    [Fact]
    public void ThePickListNumberReplacesOrdWithPl() =>
        DocumentNumber.PickList("ORD202611120001").ShouldBe("PL202611120001");

    [Fact]
    public async Task TheCounterStartsAtOneAgainOnANewDay()
    {
        using var fixture = new SalesFixture();
        var customer = await fixture.NewCustomerAsync();
        var first = await fixture.NewOfferAsync(customer.Id, SalesFixture.Line());
        var tomorrow = new CreateOffer.Handler(fixture.DbFactory, new CreateOffer.Validator(), new DayClock(new DateTimeOffset(2026, 11, 11, 12, 0, 0, TimeSpan.Zero)), [fixture.Contacts]);

        var next = await tomorrow.HandleAsync(new CreateOffer.Command(SalesFixture.Offer(customer.Id, SalesFixture.Line())), Ct);

        first.Offer.Number.ShouldBe("OFF202611100001");
        next.Value.Offer.Number.ShouldBe("OFF202611110001");
    }

    [Fact]
    public async Task AnOrderNumberHasTheOrdPrefixTheDateAndAFourDigitCounter()
    {
        using var fixture = new SalesFixture();
        var customer = await fixture.NewCustomerAsync();
        var offer = await fixture.NewOfferAsync(customer.Id, SalesFixture.Line());
        await fixture.ChangeStatus.HandleAsync(new ChangeOfferStatus.Command(offer.Offer.Id, Domain.OfferStatus.Sent), Ct);
        await fixture.ChangeStatus.HandleAsync(new ChangeOfferStatus.Command(offer.Offer.Id, Domain.OfferStatus.Accepted), Ct);

        var order = (await fixture.CreateOrder.HandleAsync(new CreateOrderFromOffer.Command(offer.Offer.Id), Ct)).Value;

        order.Order.Number.ShouldMatch(@"^ORD20261110\d{4}$");
    }

    private sealed class DayClock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}

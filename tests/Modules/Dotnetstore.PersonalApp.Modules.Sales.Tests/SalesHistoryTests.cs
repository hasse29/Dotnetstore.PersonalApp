using System.Globalization;
using Dotnetstore.PersonalApp.Modules.Sales.Features.Customers;
using Dotnetstore.PersonalApp.Modules.Sales.Features.Invoices;

namespace Dotnetstore.PersonalApp.Modules.Sales.Tests;

public sealed class SalesHistoryTests : IDisposable
{
    private readonly ScreenHarness _harness = new();

    private SalesFixture Fixture => _harness.Fixture;

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public void Dispose() => _harness.Dispose();

    private static string Money(decimal value) => value.ToString("N2", CultureInfo.CurrentCulture);

    /// <summary>
    /// Anna: a draft offer, an invoiced order that has been part-paid (500 with 200 paid), and an invoiced order whose invoice was
    /// cancelled. Beta: one offer that must never show up for Anna.
    /// </summary>
    private async Task<(CustomerDto Anna, CustomerDto Beta)> SeedAsync()
    {
        var anna = await Fixture.NewCustomerAsync("Anna AB");
        var beta = await Fixture.NewCustomerAsync("Beta AB");

        await Fixture.NewOfferAsync(anna.Id, SalesFixture.Line());
        await Fixture.NewOfferAsync(beta.Id, SalesFixture.Line());

        var paidOrder = await _harness.ServiceOrderAsync(anna.Id);
        var invoice = (await Fixture.CreateInvoice.HandleAsync(new CreateInvoiceFromOrder.Command(paidOrder.Order.Id), Ct)).Value;
        await Fixture.RecordPayment.HandleAsync(new RecordPayment.Command(invoice.Invoice.Id, 200m), Ct);

        var cancelledOrder = await _harness.ServiceOrderAsync(anna.Id);
        var cancelled = (await Fixture.CreateInvoice.HandleAsync(new CreateInvoiceFromOrder.Command(cancelledOrder.Order.Id), Ct)).Value;
        await Fixture.CancelInvoice.HandleAsync(new CancelInvoice.Command(cancelled.Invoice.Id), Ct);

        return (anna, beta);
    }

    [Fact]
    public async Task ACustomerWithNothingHasAnEmptyHistory()
    {
        var customer = await Fixture.NewCustomerAsync();

        var result = await Fixture.GetSalesHistory.HandleAsync(new GetCustomerSalesHistory.Query(customer.Id), Ct);

        result.Entries.ShouldBeEmpty();
        result.Summary.ShouldBe(new SalesHistorySummary(0, 0, 0, 0m, 0m));
        result.Summary.Outstanding.ShouldBe(0m);
    }

    [Fact]
    public async Task TheHistoryHoldsTheCustomersOffersOrdersAndInvoicesOnly()
    {
        var (anna, beta) = await SeedAsync();

        var result = await Fixture.GetSalesHistory.HandleAsync(new GetCustomerSalesHistory.Query(anna.Id), Ct);

        result.Entries.Count(e => e.Kind == SalesHistoryKind.Offer).ShouldBe(3);
        result.Entries.Count(e => e.Kind == SalesHistoryKind.Order).ShouldBe(2);
        result.Entries.Count(e => e.Kind == SalesHistoryKind.Invoice).ShouldBe(2);
        result.Summary.Offers.ShouldBe(3);
        result.Summary.Orders.ShouldBe(2);
        result.Summary.Invoices.ShouldBe(2);

        var other = await Fixture.GetSalesHistory.HandleAsync(new GetCustomerSalesHistory.Query(beta.Id), Ct);
        other.Entries.Single().Kind.ShouldBe(SalesHistoryKind.Offer);
    }

    [Fact]
    public async Task ACancelledInvoiceIsListedButCountsForNothing()
    {
        var (anna, _) = await SeedAsync();

        var result = await Fixture.GetSalesHistory.HandleAsync(new GetCustomerSalesHistory.Query(anna.Id), Ct);

        result.Entries.Count(e => e.Status == "Cancelled").ShouldBe(1);
        result.Summary.Invoiced.ShouldBe(500m);
        result.Summary.Paid.ShouldBe(200m);
        result.Summary.Outstanding.ShouldBe(300m);
    }

    [Fact]
    public async Task EachEntryCarriesItsTotalStatusAndWhereItCameFrom()
    {
        var (anna, _) = await SeedAsync();

        var result = await Fixture.GetSalesHistory.HandleAsync(new GetCustomerSalesHistory.Query(anna.Id), Ct);

        var invoice = result.Entries.First(e => e.Kind == SalesHistoryKind.Invoice && e.Status == "PartiallyPaid");
        invoice.Total.ShouldBe(500m);
        invoice.Paid.ShouldBe(200m);
        invoice.From.ShouldStartWith("ORD");
        invoice.Number.ShouldStartWith("INV");

        var order = result.Entries.First(e => e.Number == invoice.From);
        order.Kind.ShouldBe(SalesHistoryKind.Order);
        order.Status.ShouldBe("Invoiced");
        order.From.ShouldStartWith("OFF");

        result.Entries.Single(e => e.Number == order.From).Status.ShouldBe("Accepted");
        result.Entries.Where(e => e.Kind == SalesHistoryKind.Offer).ShouldContain(e => e.Status == "Draft" && e.From == string.Empty);
    }

    [Fact]
    public async Task TheNewestDocumentsComeFirst()
    {
        var (anna, _) = await SeedAsync();

        var result = await Fixture.GetSalesHistory.HandleAsync(new GetCustomerSalesHistory.Query(anna.Id), Ct);

        var expected = result.Entries.OrderByDescending(e => e.Date).ThenByDescending(e => e.Kind).ThenByDescending(e => e.Number, StringComparer.Ordinal).ToList();
        result.Entries.ShouldBe(expected);
        result.Entries[0].Kind.ShouldBe(SalesHistoryKind.Invoice);
    }

    // ---- view model ----

    private CustomerSalesHistoryViewModel Tab(Guid customerId) => new(customerId, Fixture.GetSalesHistory, _harness.Localizer);

    [Fact]
    public async Task TheTabShowsEveryDocumentWithTextsAndTheSummary()
    {
        var (anna, _) = await SeedAsync();
        var tab = Tab(anna.Id);

        await tab.LoadCommand.ExecuteAsync(null);

        tab.Entries.Count.ShouldBe(7);
        tab.InvoicedText.ShouldBe(Money(500m));
        tab.PaidText.ShouldBe(Money(200m));
        tab.OutstandingText.ShouldBe(Money(300m));
        tab.StatusMessage.ShouldBe(_harness.Localizer.Format("SalesHistory_Count", 3, 2, 2));

        var partlyPaid = tab.Entries.Single(r => r.Entry.Status == "PartiallyPaid");
        partlyPaid.KindText.ShouldBe(_harness.Localizer.Get("SalesHistory_Kind_Invoice"));
        partlyPaid.StatusText.ShouldBe(_harness.Localizer.Get("InvoiceStatus_PartiallyPaid"));
        partlyPaid.TotalText.ShouldBe(Money(500m));
        partlyPaid.FromText.ShouldStartWith("ORD");

        tab.Entries.Single(r => r.Entry.Status == "Draft").StatusText.ShouldBe(_harness.Localizer.Get("OfferStatus_Draft"));
        tab.Entries.First(r => r.Entry.Kind == SalesHistoryKind.Order).KindText.ShouldBe(_harness.Localizer.Get("SalesHistory_Kind_Order"));
    }

    [Fact]
    public async Task TheTabSaysSoWhenThereIsNothingAndReloadingDoesNotDuplicateRows()
    {
        var customer = await Fixture.NewCustomerAsync();
        var tab = Tab(customer.Id);

        await tab.LoadCommand.ExecuteAsync(null);
        tab.Entries.ShouldBeEmpty();
        tab.StatusMessage.ShouldBe(_harness.Localizer.Get("SalesHistory_Empty"));
        tab.InvoicedText.ShouldBe(Money(0m));

        await Fixture.NewOfferAsync(customer.Id, SalesFixture.Line());
        await tab.LoadCommand.ExecuteAsync(null);
        await tab.LoadCommand.ExecuteAsync(null);

        tab.Entries.Count.ShouldBe(1);
    }

    [Fact]
    public async Task TheDetailPageHasTheTabOnlyWhenItIsGivenTheHandler()
    {
        var customer = await Fixture.NewCustomerAsync();

        new CustomerDetailViewModel(customer, "Active").SalesHistory.ShouldBeNull();
        new CustomerDetailViewModel(customer, "Active", salesHistory: Fixture.GetSalesHistory, localization: _harness.Localizer).SalesHistory.ShouldNotBeNull();
    }
}

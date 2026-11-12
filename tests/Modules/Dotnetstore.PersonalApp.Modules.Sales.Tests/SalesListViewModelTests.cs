using System.Globalization;
using Dotnetstore.PersonalApp.Modules.Sales.Domain;
using Dotnetstore.PersonalApp.Modules.Sales.Features.Credits;
using Dotnetstore.PersonalApp.Modules.Sales.Features.Invoices;
using Dotnetstore.PersonalApp.Modules.Sales.Features.Offers;
using Dotnetstore.PersonalApp.Modules.Sales.Features.Orders;
using Dotnetstore.PersonalApp.Modules.Sales.Features.PickLists;

namespace Dotnetstore.PersonalApp.Modules.Sales.Tests;

/// <summary>The list screens of Sales: loading, filtering, searching, and opening and leaving a document.</summary>
public sealed class SalesListViewModelTests : IDisposable
{
    private readonly ScreenHarness _harness = new();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public void Dispose() => _harness.Dispose();

    // Leaving a screen reloads the list from an async void event handler, so tests wait for what they expect.
    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        for (var i = 0; i < 500 && !condition(); i++)
        {
            await Task.Delay(10, Ct);
        }

        condition().ShouldBeTrue();
    }

    private OffersViewModel Offers() =>
        new(
            _harness.Fixture.ListOffers,
            _harness.Fixture.CreateOffer,
            _harness.Fixture.UpdateOffer,
            _harness.Fixture.GetOffer,
            _harness.Fixture.ChangeStatus,
            _harness.Fixture.DeleteOffer,
            _harness.Fixture.CreateOrder,
            _harness.Fixture.ListCustomers,
            _harness.Catalog,
            _harness.Localizer,
            _harness.Fixture.Clock,
            _harness.Pdf);

    private OrdersViewModel Orders() =>
        new(_harness.Fixture.ListOrders, _harness.Fixture.GetOrder, _harness.Fixture.CancelOrder, _harness.Fixture.CreateInvoice, _harness.Localizer, _harness.Pdf);

    private InvoicesViewModel Invoices() =>
        new(
            _harness.Fixture.ListInvoices,
            _harness.Fixture.GetInvoice,
            _harness.Fixture.RecordPayment,
            _harness.Fixture.ApplyCredit,
            _harness.Fixture.DeletePayment,
            _harness.Fixture.CancelInvoice,
            _harness.Localizer,
            _harness.Pdf);

    private PickListsViewModel PickLists() =>
        new(_harness.Fixture.ListPickLists, _harness.Fixture.GetPickList, _harness.Fixture.SavePicked, _harness.Localizer, _harness.Pdf);

    private CreditsViewModel Credits() =>
        new(_harness.Fixture.ListCredits, _harness.Fixture.GetCredit, _harness.Fixture.RefundCredit, _harness.Localizer);

    // ---- offers ----

    [Fact]
    public async Task OffersStartsOnAllStatusesAndShowsAnEmptyCount()
    {
        var list = Offers();

        await list.LoadCommand.ExecuteAsync(null);

        list.SelectedStatus.Status.ShouldBeNull();
        list.StatusOptions.Count.ShouldBe(Enum.GetValues<OfferStatus>().Length + 1);
        list.Offers.ShouldBeEmpty();
        list.StatusMessage.ShouldBe(_harness.Localizer.Format("Offers_Count", 0));
        list.HasScreen.ShouldBeFalse();
    }

    [Fact]
    public async Task OffersShowsEachOfferWithItsFormattedTexts()
    {
        var offer = await _harness.AcceptedOfferAsync(SalesFixture.Line("Consulting", quantity: 4, price: 100));
        var list = Offers();

        await list.LoadCommand.ExecuteAsync(null);

        var row = list.Offers.Single();
        row.Offer.Id.ShouldBe(offer.Offer.Id);
        row.StatusText.ShouldBe(_harness.Localizer.Get("OfferStatus_Accepted"));
        row.TotalText.ShouldBe(500m.ToString("N2", CultureInfo.CurrentCulture));
        row.DateText.ShouldBe(offer.Offer.Date.ToString("d", CultureInfo.CurrentCulture));
        row.ValidUntilText.ShouldBe(offer.Offer.ValidUntil.ToString("d", CultureInfo.CurrentCulture));
        list.StatusMessage.ShouldBe(_harness.Localizer.Format("Offers_Count", 1));
    }

    [Fact]
    public async Task OffersCanBeFilteredByStatusAndBySearch()
    {
        var sent = await _harness.AcceptedOfferAsync(SalesFixture.Line());
        var draft = await _harness.Fixture.NewOfferAsync((await _harness.Fixture.NewCustomerAsync("Zeta Co")).Id, SalesFixture.Line());
        var list = Offers();
        await list.LoadCommand.ExecuteAsync(null);
        list.Offers.Count.ShouldBe(2);

        list.SelectedStatus = list.StatusOptions.Single(o => o.Status == OfferStatus.Draft);
        await WaitUntilAsync(() => list.Offers.Count == 1);
        list.Offers.Single().Offer.Id.ShouldBe(draft.Offer.Id);

        list.SelectedStatus = list.StatusOptions[0];
        await WaitUntilAsync(() => list.Offers.Count == 2);
        list.Search = "Zeta";
        await list.LoadCommand.ExecuteAsync(null);
        list.Offers.Single().Offer.Id.ShouldBe(draft.Offer.Id);

        list.Search = "no such customer";
        await list.LoadCommand.ExecuteAsync(null);
        list.Offers.ShouldBeEmpty();
        sent.Offer.Id.ShouldNotBe(draft.Offer.Id);
    }

    [Fact]
    public async Task NewOpensAnEmptyOfferScreenAndLeavingItWithoutSavingKeepsTheList()
    {
        var list = Offers();
        await list.LoadCommand.ExecuteAsync(null);

        list.NewCommand.Execute(null);

        list.HasScreen.ShouldBeTrue();
        list.Screen!.Exists.ShouldBeFalse();

        list.Screen.BackCommand.Execute(null);
        list.HasScreen.ShouldBeFalse();
        list.SelectedOffer.ShouldBeNull();
    }

    [Fact]
    public async Task SelectingAnOfferOpensItsScreenAndLeavingItReloadsTheList()
    {
        var offer = await _harness.AcceptedOfferAsync(SalesFixture.Line());
        var list = Offers();
        await list.LoadCommand.ExecuteAsync(null);

        list.SelectedOffer = list.Offers.Single();
        list.HasScreen.ShouldBeTrue();
        await WaitUntilAsync(() => list.Screen!.Number == offer.Offer.Number);

        // Something changes while the offer is open; leaving shows it.
        await _harness.Fixture.NewOfferAsync(offer.Offer.CustomerId, SalesFixture.Line());
        list.Screen!.BackCommand.Execute(null);

        list.HasScreen.ShouldBeFalse();
        list.SelectedOffer.ShouldBeNull();
        await WaitUntilAsync(() => list.Offers.Count == 2);
    }

    // ---- orders ----

    [Fact]
    public async Task OrdersShowStatusTotalAndPickProgress()
    {
        var service = await _harness.ServiceOrderAsync();
        var goods = await _harness.GoodsOrderAsync();
        var list = Orders();

        await list.LoadCommand.ExecuteAsync(null);

        list.Orders.Count.ShouldBe(2);
        var serviceRow = list.Orders.Single(o => o.Order.Id == service.Order.Id);
        serviceRow.PickText.ShouldBe(_harness.Localizer.Get("Orders_NothingToPick"));
        serviceRow.TotalText.ShouldBe(500m.ToString("N2", CultureInfo.CurrentCulture));
        serviceRow.StatusText.ShouldBe(_harness.Localizer.Get("OrderStatus_ReadyToInvoice"));
        list.Orders.Single(o => o.Order.Id == goods.Order.Id).PickText.ShouldBe(_harness.Localizer.Format("Orders_PickProgress", 0, 1));
        list.StatusMessage.ShouldBe(_harness.Localizer.Format("Orders_Count", 2));
    }

    [Fact]
    public async Task OrdersCanBeFilteredByStatusAndSearch()
    {
        var service = await _harness.ServiceOrderAsync();
        await _harness.GoodsOrderAsync();
        var list = Orders();
        await list.LoadCommand.ExecuteAsync(null);

        list.SelectedStatus = list.StatusOptions.Single(o => o.Status == OrderStatus.ReadyToInvoice);
        await WaitUntilAsync(() => list.Orders.Count == 1);
        list.Orders.Single().Order.Id.ShouldBe(service.Order.Id);

        list.SelectedStatus = list.StatusOptions[0];
        await WaitUntilAsync(() => list.Orders.Count == 2);
        list.Search = service.Order.Number;
        await list.LoadCommand.ExecuteAsync(null);
        list.Orders.Single().Order.Id.ShouldBe(service.Order.Id);
    }

    [Fact]
    public async Task SelectingAnOrderOpensItsScreenAndLeavingItReloadsTheList()
    {
        var order = await _harness.ServiceOrderAsync();
        var list = Orders();
        await list.LoadCommand.ExecuteAsync(null);

        list.SelectedOrder = list.Orders.Single();
        list.HasScreen.ShouldBeTrue();
        await WaitUntilAsync(() => list.Screen!.Heading == order.Order.Number);
        await list.Screen!.CreateInvoiceCommand.ExecuteAsync(null);

        list.Screen.BackCommand.Execute(null);

        list.HasScreen.ShouldBeFalse();
        list.SelectedOrder.ShouldBeNull();
        await WaitUntilAsync(() => list.Orders.Single().StatusText == _harness.Localizer.Get("OrderStatus_Invoiced"));
    }

    // ---- invoices ----

    [Fact]
    public async Task InvoicesShowTotalsAndWhatIsOutstanding()
    {
        var invoice = await _harness.InvoiceAsync();
        await _harness.Fixture.RecordPayment.HandleAsync(new RecordPayment.Command(invoice.Invoice.Id, 200), Ct);
        var list = Invoices();

        await list.LoadCommand.ExecuteAsync(null);

        var row = list.Invoices.Single();
        row.TotalText.ShouldBe(500m.ToString("N2", CultureInfo.CurrentCulture));
        row.OutstandingText.ShouldBe(300m.ToString("N2", CultureInfo.CurrentCulture));
        row.StatusText.ShouldBe(_harness.Localizer.Get("InvoiceStatus_" + row.Invoice.Status));
        list.StatusMessage.ShouldBe(_harness.Localizer.Format("Invoices_Count", 1));
    }

    [Fact]
    public async Task InvoicesCanBeFilteredByStatusAndSearch()
    {
        var unpaid = await _harness.InvoiceAsync();
        var paid = await _harness.InvoiceAsync();
        await _harness.Fixture.RecordPayment.HandleAsync(new RecordPayment.Command(paid.Invoice.Id, 500), Ct);
        var list = Invoices();
        await list.LoadCommand.ExecuteAsync(null);
        list.Invoices.Count.ShouldBe(2);

        list.SelectedStatus = list.StatusOptions.Single(o => o.Status == InvoiceStatus.Paid);
        await WaitUntilAsync(() => list.Invoices.Count == 1);
        list.Invoices.Single().Invoice.Id.ShouldBe(paid.Invoice.Id);

        list.SelectedStatus = list.StatusOptions[0];
        await WaitUntilAsync(() => list.Invoices.Count == 2);
        list.Search = unpaid.Invoice.Number;
        await list.LoadCommand.ExecuteAsync(null);
        list.Invoices.Single().Invoice.Id.ShouldBe(unpaid.Invoice.Id);
    }

    [Fact]
    public async Task SelectingAnInvoiceOpensItsScreenAndLeavingItReloadsTheList()
    {
        var invoice = await _harness.InvoiceAsync();
        var list = Invoices();
        await list.LoadCommand.ExecuteAsync(null);

        list.SelectedInvoice = list.Invoices.Single();
        list.HasScreen.ShouldBeTrue();
        await WaitUntilAsync(() => list.Screen!.Heading == invoice.Invoice.Number);
        await _harness.Fixture.RecordPayment.HandleAsync(new RecordPayment.Command(invoice.Invoice.Id, 500), Ct);

        list.Screen!.BackCommand.Execute(null);

        list.HasScreen.ShouldBeFalse();
        list.SelectedInvoice.ShouldBeNull();
        await WaitUntilAsync(() => list.Invoices.Single().Invoice.Outstanding == 0);
    }

    // ---- pick lists ----

    [Fact]
    public async Task PickListsStartOnTheOpenOnesAndShowProgress()
    {
        var order = await _harness.GoodsOrderAsync();
        var list = PickLists();

        list.SelectedStatus.Status.ShouldBe(PickListStatus.Open);
        await list.LoadCommand.ExecuteAsync(null);

        var row = list.PickLists.Single();
        row.PickList.OrderId.ShouldBe(order.Order.Id);
        row.StatusText.ShouldBe(_harness.Localizer.Get("PickListStatus_Open"));
        row.ProgressText.ShouldBe(_harness.Localizer.Format("PickLists_Progress", 0, 1));
        list.StatusMessage.ShouldBe(_harness.Localizer.Format("PickLists_Count", 1));
    }

    [Fact]
    public async Task AFinishedPickListLeavesTheOpenFilterButShowsUnderAll()
    {
        var order = await _harness.GoodsOrderAsync();
        var id = await _harness.PickListIdAsync(order.Order.Id);
        var screen = _harness.PickListScreen(id);
        await screen.LoadCommand.ExecuteAsync(null);
        screen.PickAllCommand.Execute(null);
        await screen.SaveCommand.ExecuteAsync(null);
        var list = PickLists();

        await list.LoadCommand.ExecuteAsync(null);
        list.PickLists.ShouldBeEmpty();
        list.StatusMessage.ShouldBe(_harness.Localizer.Format("PickLists_Count", 0));

        list.SelectedStatus = list.StatusOptions[0];
        await WaitUntilAsync(() => list.PickLists.Count == 1);
        list.PickLists.Single().StatusText.ShouldBe(_harness.Localizer.Get("PickListStatus_Complete"));
    }

    [Fact]
    public async Task SelectingAPickListOpensItsScreenAndLeavingItReloadsTheList()
    {
        var order = await _harness.GoodsOrderAsync();
        var list = PickLists();
        await list.LoadCommand.ExecuteAsync(null);

        list.SelectedPickList = list.PickLists.Single();
        list.HasScreen.ShouldBeTrue();
        await WaitUntilAsync(() => list.Screen!.Lines.Count == 1);
        list.Screen!.PickAllCommand.Execute(null);
        await list.Screen.SaveCommand.ExecuteAsync(null);

        list.Screen.BackCommand.Execute(null);

        list.HasScreen.ShouldBeFalse();
        list.SelectedPickList.ShouldBeNull();
        await WaitUntilAsync(() => list.PickLists.Count == 0);
        order.Order.Id.ShouldNotBe(Guid.Empty);
    }

    // ---- credits ----

    private async Task<Guid> CustomerWithCreditAsync()
    {
        var invoice = await _harness.InvoiceAsync();
        await _harness.Fixture.RecordPayment.HandleAsync(new RecordPayment.Command(invoice.Invoice.Id, 750), Ct);
        return (await _harness.Fixture.ListCredits.HandleAsync(Ct)).Single().CustomerId;
    }

    [Fact]
    public async Task NoCreditShowsAnEmptyList()
    {
        var list = Credits();

        await list.LoadCommand.ExecuteAsync(null);

        list.IsEmpty.ShouldBeTrue();
        list.HasSelection.ShouldBeFalse();
        list.StatusMessage.ShouldBe(_harness.Localizer.Format("Credits_Count", 0));
    }

    [Fact]
    public async Task OverpaidMoneyShowsAsCreditAndSelectingItShowsTheLedgerAndProposesARefundOfTheWholeBalance()
    {
        await CustomerWithCreditAsync();
        var list = Credits();
        await list.LoadCommand.ExecuteAsync(null);

        list.IsEmpty.ShouldBeFalse();
        list.Credits.Single().BalanceText.ShouldBe(250m.ToString("N2", CultureInfo.CurrentCulture));

        list.SelectedCredit = list.Credits.Single();
        await WaitUntilAsync(() => list.Entries.Count > 0);

        list.HasSelection.ShouldBeTrue();
        list.RefundAmount.ShouldBe(250);
        list.Entries.Single().AmountText.ShouldBe(250m.ToString("N2", CultureInfo.CurrentCulture));
    }

    [Fact]
    public async Task RefundingPartOfTheCreditLowersTheBalanceAndKeepsTheSelection()
    {
        var customerId = await CustomerWithCreditAsync();
        var list = Credits();
        await list.LoadCommand.ExecuteAsync(null);
        list.SelectedCredit = list.Credits.Single();
        list.RefundAmount = 100;
        list.RefundNote = "Bank transfer";

        await list.RefundCommand.ExecuteAsync(null);

        list.StatusMessage.ShouldBe(_harness.Localizer.Format("Credits_Refunded", 100m.ToString("N2", CultureInfo.CurrentCulture)));
        list.SelectedCredit.ShouldNotBeNull().Credit.CustomerId.ShouldBe(customerId);
        list.SelectedCredit.BalanceText.ShouldBe(150m.ToString("N2", CultureInfo.CurrentCulture));
        list.RefundNote.ShouldBeNull();
        list.Entries.Count.ShouldBe(2);
    }

    [Fact]
    public async Task RefundingEverythingRemovesTheCustomerFromTheList()
    {
        await CustomerWithCreditAsync();
        var list = Credits();
        await list.LoadCommand.ExecuteAsync(null);
        list.SelectedCredit = list.Credits.Single();

        await list.RefundCommand.ExecuteAsync(null);

        list.IsEmpty.ShouldBeTrue();
        list.SelectedCredit.ShouldBeNull();
        list.Entries.ShouldBeEmpty();
    }

    [Fact]
    public async Task RefundingMoreThanTheCreditIsRejectedAndNothingChanges()
    {
        await CustomerWithCreditAsync();
        var list = Credits();
        await list.LoadCommand.ExecuteAsync(null);
        list.SelectedCredit = list.Credits.Single();
        list.RefundAmount = 1000;

        await list.RefundCommand.ExecuteAsync(null);

        list.StatusMessage.ShouldNotBeNull().ShouldNotBe(_harness.Localizer.Format("Credits_Refunded", 1000m.ToString("N2", CultureInfo.CurrentCulture)));
        list.Credits.Single().BalanceText.ShouldBe(250m.ToString("N2", CultureInfo.CurrentCulture));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public async Task RefundingZeroOrANegativeAmountIsRejected(int amount)
    {
        await CustomerWithCreditAsync();
        var list = Credits();
        await list.LoadCommand.ExecuteAsync(null);
        list.SelectedCredit = list.Credits.Single();
        list.RefundAmount = amount;

        await list.RefundCommand.ExecuteAsync(null);

        list.Credits.Single().BalanceText.ShouldBe(250m.ToString("N2", CultureInfo.CurrentCulture));
        list.StatusMessage.ShouldNotStartWith("Refunded");
    }

    [Fact]
    public async Task AnEmptyRefundAmountSaysSoAndNothingIsRefunded()
    {
        await CustomerWithCreditAsync();
        var list = Credits();
        await list.LoadCommand.ExecuteAsync(null);
        list.SelectedCredit = list.Credits.Single();
        list.RefundAmount = null;

        await list.RefundCommand.ExecuteAsync(null);

        list.StatusMessage.ShouldBe(_harness.Localizer.Get("Error_payment_amount_invalid"));
        list.Credits.Single().BalanceText.ShouldBe(250m.ToString("N2", CultureInfo.CurrentCulture));
    }

    [Fact]
    public async Task RefundingWithoutASelectionDoesNothing()
    {
        await CustomerWithCreditAsync();
        var list = Credits();
        await list.LoadCommand.ExecuteAsync(null);
        list.RefundAmount = 50;

        await list.RefundCommand.ExecuteAsync(null);

        list.StatusMessage.ShouldBe(_harness.Localizer.Format("Credits_Count", 1));
        list.Credits.Single().BalanceText.ShouldBe(250m.ToString("N2", CultureInfo.CurrentCulture));
    }
}

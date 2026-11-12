using System.Globalization;
using Dotnetstore.PersonalApp.Modules.Sales.Domain;
using Dotnetstore.PersonalApp.Modules.Sales.Features.Offers;
using Dotnetstore.PersonalApp.Modules.Sales.Features.Customers;
using Dotnetstore.PersonalApp.Shared.Products;

namespace Dotnetstore.PersonalApp.Modules.Sales.Tests;

public sealed class OfferScreenViewModelTests : IDisposable
{
    private readonly ScreenHarness _harness = new();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public void Dispose() => _harness.Dispose();

    private static string Money(decimal amount) => amount.ToString("N2", CultureInfo.CurrentCulture);

    private string Error(string code) => _harness.Localizer.Get("Error_" + code);

    private async Task<OfferScreenViewModel> NewScreenWithCustomerAsync()
    {
        var customer = await _harness.Fixture.NewCustomerAsync("Acme School");
        var screen = _harness.OfferScreen();
        await screen.LoadCommand.ExecuteAsync(null);
        screen.SelectedCustomer = screen.Customers.Single(c => c.Id == customer.Id);
        return screen;
    }

    private async Task<OfferScreenViewModel> SavedDraftAsync()
    {
        var screen = await NewScreenWithCustomerAsync();
        screen.AddFreeLineCommand.Execute(null);
        var line = screen.Lines.Single();
        line.Description = "Consulting";
        line.Quantity = 4;
        line.UnitPrice = 100;
        line.VatRate = 25;
        await screen.SaveCommand.ExecuteAsync(null);
        screen.ErrorMessage.ShouldBeNull();
        return screen;
    }

    [Fact]
    public async Task ANewOfferStartsEmptyWithTodayAndThirtyDaysAsDates()
    {
        var screen = _harness.OfferScreen();
        var today = DateOnly.FromDateTime(_harness.Fixture.Clock.GetLocalNow().DateTime);

        screen.Heading.ShouldBe(_harness.Localizer.Get("OfferScreen_New"));
        screen.Exists.ShouldBeFalse();
        screen.IsEditable.ShouldBeTrue();
        screen.ShowSend.ShouldBeFalse();
        screen.ShowDelete.ShouldBeFalse();
        screen.Lines.ShouldBeEmpty();
        DateOnly.FromDateTime(screen.Date!.Value).ShouldBe(today);
        DateOnly.FromDateTime(screen.ValidUntil!.Value).ShouldBe(today.AddDays(30));
    }

    [Fact]
    public async Task SavingWithoutDatesIsRejectedBeforeAnythingIsStored()
    {
        var screen = await NewScreenWithCustomerAsync();
        screen.Date = null;

        await screen.SaveCommand.ExecuteAsync(null);

        screen.ErrorMessage.ShouldBe(Error("offer_date_required"));
        screen.Exists.ShouldBeFalse();
        screen.StatusMessage.ShouldBeNull();
    }

    [Fact]
    public async Task SavingWithoutACustomerIsRejected()
    {
        var screen = _harness.OfferScreen();
        await screen.LoadCommand.ExecuteAsync(null);

        await screen.SaveCommand.ExecuteAsync(null);

        screen.ErrorMessage.ShouldBe(Error("offer_customer_required"));
        screen.Exists.ShouldBeFalse();
    }

    [Fact]
    public async Task ValidUntilBeforeTheDateIsRejected()
    {
        var screen = await NewScreenWithCustomerAsync();
        screen.ValidUntil = screen.Date!.Value.AddDays(-1);

        await screen.SaveCommand.ExecuteAsync(null);

        screen.ErrorMessage.ShouldBe(Error("offer_valid_until_before_date"));
        screen.Exists.ShouldBeFalse();
    }

    [Fact]
    public async Task ALineWithoutDescriptionIsRejected()
    {
        var screen = await NewScreenWithCustomerAsync();
        screen.AddFreeLineCommand.Execute(null);
        screen.Lines.Single().UnitPrice = 10;

        await screen.SaveCommand.ExecuteAsync(null);

        screen.ErrorMessage.ShouldBe(Error("offer_line_description_required"));
        screen.Exists.ShouldBeFalse();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-2)]
    public async Task ALineWithNoQuantityOrLessIsRejected(int quantity)
    {
        var screen = await NewScreenWithCustomerAsync();
        screen.AddFreeLineCommand.Execute(null);
        screen.Lines.Single().Description = "Work";
        screen.Lines.Single().Quantity = quantity;

        await screen.SaveCommand.ExecuteAsync(null);

        screen.ErrorMessage.ShouldBe(Error("offer_line_quantity_invalid"));
    }

    [Fact]
    public async Task AnEmptyQuantityFieldCountsAsZeroAndIsRejected()
    {
        var screen = await NewScreenWithCustomerAsync();
        screen.AddFreeLineCommand.Execute(null);
        screen.Lines.Single().Description = "Work";
        screen.Lines.Single().Quantity = null;

        await screen.SaveCommand.ExecuteAsync(null);

        screen.ErrorMessage.ShouldBe(Error("offer_line_quantity_invalid"));
    }

    [Fact]
    public async Task SavingANewOfferGivesItANumberAndKeepsTheLines()
    {
        var screen = await SavedDraftAsync();

        screen.Exists.ShouldBeTrue();
        screen.Number.ShouldNotBeEmpty();
        screen.Heading.ShouldBe(screen.Number);
        screen.StatusMessage.ShouldBe(_harness.Localizer.Get("OfferScreen_Saved"));
        screen.StatusText.ShouldBe(_harness.Localizer.Get("OfferStatus_Draft"));
        screen.Lines.Single().Description.ShouldBe("Consulting");
        screen.ShowSend.ShouldBeTrue();
        screen.ShowDelete.ShouldBeTrue();
    }

    [Fact]
    public async Task SavingAnExistingOfferUpdatesItInsteadOfCreatingAnother()
    {
        var screen = await SavedDraftAsync();
        var number = screen.Number;
        screen.Notes = "Changed";
        screen.Lines.Single().Quantity = 8;

        await screen.SaveCommand.ExecuteAsync(null);

        screen.Number.ShouldBe(number);
        screen.NetText.ShouldBe(Money(800));
        var offers = await _harness.Fixture.ListOffers.HandleAsync(new ListOffers.Query(), Ct);
        offers.TotalCount.ShouldBe(1);
    }

    [Fact]
    public async Task TheTotalsFollowTheLinesAsTheyAreEdited()
    {
        var screen = _harness.OfferScreen();
        screen.NetText.ShouldBe(Money(0));
        screen.AddFreeLineCommand.Execute(null);
        var line = screen.Lines.Single();

        line.Quantity = 4;
        line.UnitPrice = 100;
        line.VatRate = 25;

        screen.HasLines.ShouldBeTrue();
        line.LineTotalText.ShouldBe(Money(400));
        screen.NetText.ShouldBe(Money(400));
        screen.VatText.ShouldBe(Money(100));
        screen.TotalText.ShouldBe(Money(500));

        line.DiscountPercent = 50;
        screen.NetText.ShouldBe(Money(200));
        screen.TotalText.ShouldBe(Money(250));
    }

    [Fact]
    public async Task RemovingALineUpdatesTheTotalsAndRemovingNothingIsHarmless()
    {
        var screen = _harness.OfferScreen();
        screen.AddFreeLineCommand.Execute(null);
        screen.AddFreeLineCommand.Execute(null);
        foreach (var line in screen.Lines)
        {
            line.Quantity = 1;
            line.UnitPrice = 100;
            line.VatRate = 0;
        }

        screen.RemoveLineCommand.Execute(null);
        screen.Lines.Count.ShouldBe(2);

        screen.RemoveLineCommand.Execute(screen.Lines[0]);

        screen.Lines.Count.ShouldBe(1);
        screen.NetText.ShouldBe(Money(100));
        screen.RemoveLineCommand.Execute(screen.Lines[0]);
        screen.HasLines.ShouldBeFalse();
        screen.TotalText.ShouldBe(Money(0));
    }

    [Fact]
    public async Task SearchingProductsListsTheMatchesAndNothingForAMiss()
    {
        _harness.Catalog.Products.Add(new ProductSummary(Guid.NewGuid(), "P-1", "Cat6 cable", "Piece", 50, 25, true));
        _harness.Catalog.Products.Add(new ProductSummary(Guid.NewGuid(), "P-2", "Router", "Piece", 900, 25, true));
        var screen = _harness.OfferScreen();

        screen.ProductSearch = "cable";
        await screen.SearchProductsCommand.ExecuteAsync(null);
        screen.ProductResults.Select(p => p.Number).ShouldBe(["P-1"]);
        screen.HasProductResults.ShouldBeTrue();

        screen.ProductSearch = "nothing like this";
        await screen.SearchProductsCommand.ExecuteAsync(null);
        screen.ProductResults.ShouldBeEmpty();
        screen.HasProductResults.ShouldBeFalse();
    }

    [Fact]
    public async Task ChoosingAProductAddsItsLineAndClearsTheSearch()
    {
        var product = new ProductSummary(Guid.NewGuid(), "P-1", "Cat6 cable", "Piece", 50, 25, true);
        _harness.Catalog.Products.Add(product);
        var screen = _harness.OfferScreen();
        screen.ProductSearch = "cable";
        await screen.SearchProductsCommand.ExecuteAsync(null);

        screen.SelectedProduct = product;

        var line = screen.Lines.Single();
        line.Description.ShouldBe("P-1 Cat6 cable");
        line.ProductId.ShouldBe(product.Id);
        line.IsPhysical.ShouldBeTrue();
        line.UnitPrice.ShouldBe(50);
        line.VatRate.ShouldBe(25);
        line.SelectedUnit.Key.ShouldBe("Piece");
        screen.ProductResults.ShouldBeEmpty();
        screen.ProductSearch.ShouldBeEmpty();
        screen.SelectedProduct.ShouldBeNull();
        screen.NetText.ShouldBe(Money(50));
    }

    [Fact]
    public async Task AProductWithAnUnknownUnitStillGetsALineWithThatUnit()
    {
        var product = new ProductSummary(Guid.NewGuid(), "P-9", "Rope", "Fathom", 10, 12, false);
        var screen = _harness.OfferScreen();

        screen.SelectedProduct = product;

        var line = screen.Lines.Single();
        line.SelectedUnit.Key.ShouldBe("Fathom");
        line.IsPhysical.ShouldBeFalse();
        line.Units.ShouldContain(u => u.Key == "Fathom");
    }

    [Fact]
    public async Task SendingSavesWhatIsOnScreenFirstAndMarksTheOfferSent()
    {
        var screen = await SavedDraftAsync();
        screen.Lines.Single().Quantity = 6;

        await screen.SendCommand.ExecuteAsync(null);

        screen.ErrorMessage.ShouldBeNull();
        screen.Status.ShouldBe(OfferStatus.Sent);
        screen.StatusMessage.ShouldBe(_harness.Localizer.Format("OfferScreen_StatusChanged", screen.StatusText));
        screen.IsEditable.ShouldBeFalse();
        screen.Lines.Single().IsEditable.ShouldBeFalse();
        screen.Lines.Single().Quantity.ShouldBe(6);
        screen.ShowAnswer.ShouldBeTrue();
        screen.ShowSend.ShouldBeFalse();
    }

    [Fact]
    public async Task AnOfferWithoutLinesCannotBeSent()
    {
        var screen = await NewScreenWithCustomerAsync();
        await screen.SaveCommand.ExecuteAsync(null);

        await screen.SendCommand.ExecuteAsync(null);

        screen.ErrorMessage.ShouldBe(Error("offer_no_lines"));
        screen.Status.ShouldBe(OfferStatus.Draft);
    }

    [Fact]
    public async Task SendingAnUnsavedOfferDoesNothing()
    {
        var screen = await NewScreenWithCustomerAsync();

        await screen.SendCommand.ExecuteAsync(null);

        screen.Exists.ShouldBeFalse();
        screen.ErrorMessage.ShouldBeNull();
        (await _harness.Fixture.ListOffers.HandleAsync(new ListOffers.Query(), Ct)).TotalCount.ShouldBe(0);
    }

    [Fact]
    public async Task SendingStopsWhenTheScreenCannotBeSaved()
    {
        var screen = await SavedDraftAsync();
        screen.Lines.Single().Quantity = 0;

        await screen.SendCommand.ExecuteAsync(null);

        screen.ErrorMessage.ShouldBe(Error("offer_line_quantity_invalid"));
        screen.Status.ShouldBe(OfferStatus.Draft);
    }

    [Fact]
    public async Task AcceptingASentOfferLetsTheUserCreateTheOrder()
    {
        var screen = await SavedDraftAsync();
        await screen.SendCommand.ExecuteAsync(null);

        await screen.AcceptCommand.ExecuteAsync(null);
        screen.Status.ShouldBe(OfferStatus.Accepted);
        screen.ShowCreateOrder.ShouldBeTrue();
        screen.HasOrder.ShouldBeFalse();

        await screen.CreateOrderCommand.ExecuteAsync(null);

        screen.OrderNumber.ShouldNotBeNull();
        screen.HasOrder.ShouldBeTrue();
        screen.ShowCreateOrder.ShouldBeFalse();
        screen.StatusMessage.ShouldBe(_harness.Localizer.Format("OfferScreen_OrderCreated", screen.OrderNumber));
        screen.OrderText.ShouldBe(_harness.Localizer.Format("OfferScreen_HasOrder", screen.OrderNumber));
    }

    [Fact]
    public async Task ADraftCannotBeMadeIntoAnOrder()
    {
        var screen = await SavedDraftAsync();

        await screen.CreateOrderCommand.ExecuteAsync(null);

        screen.ErrorMessage.ShouldBe(Error("order_offer_not_accepted"));
        screen.HasOrder.ShouldBeFalse();
    }

    [Fact]
    public async Task CreatingAnOrderFromAnUnsavedOfferDoesNothing()
    {
        var screen = _harness.OfferScreen();

        await screen.CreateOrderCommand.ExecuteAsync(null);

        screen.ErrorMessage.ShouldBeNull();
        screen.StatusMessage.ShouldBeNull();
    }

    [Fact]
    public async Task ADeclinedOfferCanBeReopenedAndEditedAgain()
    {
        var screen = await SavedDraftAsync();
        await screen.SendCommand.ExecuteAsync(null);

        await screen.DeclineCommand.ExecuteAsync(null);
        screen.Status.ShouldBe(OfferStatus.Declined);
        screen.ShowReopen.ShouldBeTrue();
        screen.ShowCreateOrder.ShouldBeFalse();

        await screen.ReopenCommand.ExecuteAsync(null);
        screen.Status.ShouldBe(OfferStatus.Draft);
        screen.IsEditable.ShouldBeTrue();
        screen.Lines.Single().IsEditable.ShouldBeTrue();
    }

    [Fact]
    public async Task AcceptingADraftIsRejected()
    {
        var screen = await SavedDraftAsync();

        await screen.AcceptCommand.ExecuteAsync(null);

        screen.ErrorMessage.ShouldBe(Error("offer_status_invalid"));
        screen.Status.ShouldBe(OfferStatus.Draft);
    }

    [Fact]
    public async Task DeletingNeedsAConfirmationThatCanBeWithdrawn()
    {
        var screen = await SavedDraftAsync();
        var closed = false;
        screen.Closed += (_, _) => closed = true;

        screen.RequestDeleteCommand.Execute(null);
        screen.IsConfirmingDelete.ShouldBeTrue();
        screen.CancelDeleteCommand.Execute(null);

        screen.IsConfirmingDelete.ShouldBeFalse();
        closed.ShouldBeFalse();
        (await _harness.Fixture.ListOffers.HandleAsync(new ListOffers.Query(), Ct)).TotalCount.ShouldBe(1);
    }

    [Fact]
    public async Task ConfirmingTheDeleteRemovesTheOfferAndAsksForAReload()
    {
        var screen = await SavedDraftAsync();
        bool? refresh = null;
        screen.Closed += (_, value) => refresh = value;
        screen.RequestDeleteCommand.Execute(null);

        await screen.ConfirmDeleteCommand.ExecuteAsync(null);

        refresh.ShouldBe(true);
        (await _harness.Fixture.ListOffers.HandleAsync(new ListOffers.Query(), Ct)).TotalCount.ShouldBe(0);
    }

    [Fact]
    public async Task ASentOfferCannotBeDeleted()
    {
        var screen = await SavedDraftAsync();
        await screen.SendCommand.ExecuteAsync(null);
        var closed = false;
        screen.Closed += (_, _) => closed = true;
        screen.ShowDelete.ShouldBeFalse();

        await screen.ConfirmDeleteCommand.ExecuteAsync(null);

        screen.ErrorMessage.ShouldBe(Error("offer_not_deletable"));
        closed.ShouldBeFalse();
        screen.IsConfirmingDelete.ShouldBeFalse();
    }

    [Fact]
    public async Task DeletingAnUnsavedOfferDoesNothing()
    {
        var screen = _harness.OfferScreen();
        var closed = false;
        screen.Closed += (_, _) => closed = true;

        await screen.ConfirmDeleteCommand.ExecuteAsync(null);

        closed.ShouldBeFalse();
        screen.ErrorMessage.ShouldBeNull();
    }

    [Fact]
    public async Task BackAsksForAReloadOnlyForAnExistingOffer()
    {
        var existing = await _harness.Fixture.NewOfferAsync(
            (await _harness.Fixture.NewCustomerAsync("Beta")).Id,
            SalesFixture.Line());
        var openedExisting = _harness.OfferScreen(existing.Offer.Id);
        var openedNew = _harness.OfferScreen();
        bool? existingRefresh = null;
        bool? newRefresh = null;
        openedExisting.Closed += (_, value) => existingRefresh = value;
        openedNew.Closed += (_, value) => newRefresh = value;

        openedExisting.BackCommand.Execute(null);
        openedNew.BackCommand.Execute(null);

        existingRefresh.ShouldBe(true);
        newRefresh.ShouldBe(false);
    }

    [Fact]
    public async Task LoadingAnExistingOfferShowsItAllIncludingItsOrderNumber()
    {
        var offer = await _harness.AcceptedOfferAsync(SalesFixture.Line("Consulting", quantity: 4, price: 100));
        var order = (await _harness.Fixture.CreateOrder.HandleAsync(new Features.Orders.CreateOrderFromOffer.Command(offer.Offer.Id), Ct)).Value;
        var screen = _harness.OfferScreen(offer.Offer.Id);

        await screen.LoadCommand.ExecuteAsync(null);

        screen.Heading.ShouldBe(offer.Offer.Number);
        screen.Status.ShouldBe(OfferStatus.Accepted);
        screen.SelectedCustomer!.Id.ShouldBe(offer.Offer.CustomerId);
        screen.OrderNumber.ShouldBe(order.Order.Number);
        screen.HasOrder.ShouldBeTrue();
        screen.ShowCreateOrder.ShouldBeFalse();
        screen.NetText.ShouldBe(Money(400));
        screen.IsEditable.ShouldBeFalse();
    }

    [Fact]
    public async Task AnUnknownOfferShowsTheError()
    {
        var screen = _harness.OfferScreen(Guid.NewGuid());

        await screen.LoadCommand.ExecuteAsync(null);

        screen.ErrorMessage.ShouldBe(Error("offer_not_found"));
        screen.Lines.ShouldBeEmpty();
    }

    [Fact]
    public async Task AnInactiveCustomerIsNotPickableButStillShownOnTheirExistingOffer()
    {
        var customer = await _harness.Fixture.NewCustomerAsync("Old Customer");
        var offer = await _harness.Fixture.NewOfferAsync(customer.Id, SalesFixture.Line());
        await _harness.Fixture.UpdateCustomer.HandleAsync(new Features.Customers.UpdateCustomer.Command(customer.Id, SalesFixture.Customer("Old Customer") with { IsActive = false }), Ct);
        var fresh = _harness.OfferScreen();
        var existing = _harness.OfferScreen(offer.Offer.Id);

        await fresh.LoadCommand.ExecuteAsync(null);
        await existing.LoadCommand.ExecuteAsync(null);

        fresh.Customers.ShouldNotContain(c => c.Id == customer.Id);
        existing.SelectedCustomer!.Id.ShouldBe(customer.Id);
        existing.SelectedCustomer.Name.ShouldBe("Old Customer");
    }

    [Fact]
    public async Task ExportPdfIsOnlyForAnExistingOffer()
    {
        var unsaved = _harness.OfferScreen();
        await unsaved.ExportPdfCommand.ExecuteAsync(null);
        _harness.FileSave.Content.ShouldBeNull();
        unsaved.StatusMessage.ShouldBeNull();

        var saved = await SavedDraftAsync();
        await saved.ExportPdfCommand.ExecuteAsync(null);

        _harness.FileSave.Content.ShouldNotBeNull().Length.ShouldBeGreaterThan(100);
        saved.StatusMessage.ShouldBe(_harness.Localizer.Format("SalesPdf_Saved", @"C:\temp\document.pdf"));
    }
}

using System.Globalization;
using Dotnetstore.PersonalApp.Modules.Sales.Features.Invoices;
using Dotnetstore.PersonalApp.Modules.Sales.Features.PickLists;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Results;

namespace Dotnetstore.PersonalApp.Modules.Sales.Tests;

/// <summary>Pick lists of one product, 10 pieces ordered, 100 in stock unless a test says otherwise.</summary>
public sealed class PickListScreenViewModelTests : IDisposable
{
    private readonly ScreenHarness _harness = new();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public void Dispose() => _harness.Dispose();

    private async Task<(PickListScreenViewModel Screen, Guid OrderId, Guid ProductId)> OpenAsync(decimal inStock = 100)
    {
        var order = await _harness.GoodsOrderAsync(10, inStock);
        var pickListId = await _harness.PickListIdAsync(order.Order.Id);
        var detail = (await _harness.Fixture.GetPickList.HandleAsync(new GetPickList.Query(pickListId), Ct)).Value;
        var screen = _harness.PickListScreen(pickListId);
        await screen.LoadCommand.ExecuteAsync(null);
        return (screen, order.Order.Id, detail.Lines.Single().ProductId!.Value);
    }

    private string Described(string code, string message) => _harness.Localizer.Describe(Error.Validation(code, message));

    [Fact]
    public async Task LoadShowsTheListItsOrderAndWhatIsOrdered()
    {
        var (screen, _, _) = await OpenAsync();

        screen.Heading.ShouldStartWith("PL20261110");
        screen.Heading.ShouldContain("ORD20261110");
        screen.CustomerName.ShouldNotBeEmpty();
        screen.StatusText.ShouldBe(_harness.Localizer.Get("PickListStatus_Open"));
        screen.IsEditable.ShouldBeTrue();
        var line = screen.Lines.Single();
        line.IsEditable.ShouldBeTrue();
        line.PickedQuantity.ShouldBe(0);
        line.OrderedText.ShouldStartWith(string.Create(CultureInfo.CurrentCulture, $"{10:0.##}"));
        line.RemainingText.ShouldBe(string.Create(CultureInfo.CurrentCulture, $"{10:0.##}"));
    }

    [Fact]
    public async Task AnUnknownListShowsTheErrorAndNothingElse()
    {
        var screen = _harness.PickListScreen(Guid.NewGuid());

        await screen.LoadCommand.ExecuteAsync(null);

        screen.StatusMessage.ShouldBe(_harness.Localizer.Get("Error_picklist_not_found"));
        screen.Lines.ShouldBeEmpty();
        screen.Heading.ShouldBeEmpty();
        screen.IsEditable.ShouldBeFalse();
    }

    [Fact]
    public async Task TypingAPickedQuantityUpdatesWhatIsLeft()
    {
        var (screen, _, _) = await OpenAsync();
        var line = screen.Lines.Single();

        line.PickedQuantity = 4;

        line.RemainingText.ShouldBe(string.Create(CultureInfo.CurrentCulture, $"{6:0.##}"));
        line.PickedQuantity = null;
        line.RemainingText.ShouldBe(string.Create(CultureInfo.CurrentCulture, $"{10:0.##}"));
        line.PickedQuantity = 10;
        line.RemainingText.ShouldBeEmpty();
    }

    [Fact]
    public async Task PickAllFillsInEverythingThatIsOrdered()
    {
        var (screen, _, _) = await OpenAsync();

        screen.PickAllCommand.Execute(null);

        screen.Lines.Single().PickedQuantity.ShouldBe(10);
        screen.Lines.Single().RemainingText.ShouldBeEmpty();
    }

    [Fact]
    public async Task SavingAPartPickTakesItOutOfStockAndKeepsTheListOpen()
    {
        var (screen, _, productId) = await OpenAsync();
        screen.Lines.Single().PickedQuantity = 4;

        await screen.SaveCommand.ExecuteAsync(null);

        screen.StatusMessage.ShouldBe(_harness.Localizer.Get("PickListScreen_Saved"));
        screen.StatusText.ShouldBe(_harness.Localizer.Get("PickListStatus_Open"));
        screen.Lines.Single().PickedQuantity.ShouldBe(4);
        _harness.Fixture.Stock.Total(productId).ShouldBe(96);
    }

    [Fact]
    public async Task PickingEverythingCompletesTheListAndTheOrderIsReadyToInvoice()
    {
        var (screen, orderId, productId) = await OpenAsync();
        screen.PickAllCommand.Execute(null);

        await screen.SaveCommand.ExecuteAsync(null);

        screen.StatusMessage.ShouldBe(_harness.Localizer.Get("PickListScreen_Complete"));
        screen.StatusText.ShouldBe(_harness.Localizer.Get("PickListStatus_Complete"));
        _harness.Fixture.Stock.Total(productId).ShouldBe(90);
        var invoice = await _harness.Fixture.CreateInvoice.HandleAsync(new CreateInvoiceFromOrder.Command(orderId), Ct);
        invoice.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task PickingLessAgainPutsTheGoodsBack()
    {
        var (screen, _, productId) = await OpenAsync();
        screen.Lines.Single().PickedQuantity = 8;
        await screen.SaveCommand.ExecuteAsync(null);

        screen.Lines.Single().PickedQuantity = 3;
        await screen.SaveCommand.ExecuteAsync(null);

        screen.Lines.Single().PickedQuantity.ShouldBe(3);
        _harness.Fixture.Stock.Total(productId).ShouldBe(97);
    }

    [Fact]
    public async Task AnEmptyFieldCountsAsNothingPicked()
    {
        var (screen, _, productId) = await OpenAsync();
        screen.Lines.Single().PickedQuantity = 5;
        await screen.SaveCommand.ExecuteAsync(null);

        screen.Lines.Single().PickedQuantity = null;
        await screen.SaveCommand.ExecuteAsync(null);

        screen.Lines.Single().PickedQuantity.ShouldBe(0);
        _harness.Fixture.Stock.Total(productId).ShouldBe(100);
    }

    [Theory]
    [InlineData(11)]
    [InlineData(-1)]
    public async Task APickedQuantityOutsideWhatWasOrderedIsRejectedAndNothingChanges(int picked)
    {
        var (screen, _, productId) = await OpenAsync();
        screen.Lines.Single().PickedQuantity = picked;

        await screen.SaveCommand.ExecuteAsync(null);

        screen.StatusMessage.ShouldBe(Described("picklist_picked_invalid", "The picked quantity must be between 0 and the ordered quantity."));
        screen.Lines.Single().PickedQuantity.ShouldBe(0);
        _harness.Fixture.Stock.Total(productId).ShouldBe(100);
    }

    [Fact]
    public async Task NotEnoughInStockFailsTheSaveAndTheScreenShowsWhatIsReallyPicked()
    {
        var (screen, _, productId) = await OpenAsync(inStock: 5);
        screen.PickAllCommand.Execute(null);

        await screen.SaveCommand.ExecuteAsync(null);

        screen.StatusMessage.ShouldNotBeNull().ShouldNotBe(_harness.Localizer.Get("PickListScreen_Complete"));
        screen.StatusMessage.ShouldNotBe(_harness.Localizer.Get("PickListScreen_Saved"));
        screen.Lines.Single().PickedQuantity.ShouldBe(0);
        screen.StatusText.ShouldBe(_harness.Localizer.Get("PickListStatus_Open"));
        _harness.Fixture.Stock.Total(productId).ShouldBe(5);
    }

    [Fact]
    public async Task ACancelledOrdersListCanBeSeenButNotChanged()
    {
        var (screen, orderId, productId) = await OpenAsync();
        await _harness.Fixture.CancelOrder.HandleAsync(new Features.Orders.CancelOrder.Command(orderId), Ct);
        await screen.LoadCommand.ExecuteAsync(null);

        screen.IsEditable.ShouldBeFalse();
        screen.Lines.Single().IsEditable.ShouldBeFalse();
        screen.StatusText.ShouldBe(_harness.Localizer.Get("PickListStatus_Cancelled"));

        screen.Lines.Single().PickedQuantity = 5;
        await screen.SaveCommand.ExecuteAsync(null);

        screen.StatusMessage.ShouldBe(Described("picklist_cancelled", "The pick list is cancelled."));
        _harness.Fixture.Stock.Total(productId).ShouldBe(100);
    }

    [Fact]
    public async Task OnceTheOrderIsInvoicedThePickingIsLocked()
    {
        var (screen, orderId, _) = await OpenAsync();
        screen.PickAllCommand.Execute(null);
        await screen.SaveCommand.ExecuteAsync(null);
        await _harness.Fixture.CreateInvoice.HandleAsync(new CreateInvoiceFromOrder.Command(orderId), Ct);
        await screen.LoadCommand.ExecuteAsync(null);

        screen.IsEditable.ShouldBeFalse();
        screen.Lines.Single().IsEditable.ShouldBeFalse();

        screen.Lines.Single().PickedQuantity = 2;
        await screen.SaveCommand.ExecuteAsync(null);

        screen.StatusMessage.ShouldBe(Described("picklist_locked", "The order has been invoiced, so the picking cannot be changed."));
        screen.Lines.Single().PickedQuantity.ShouldBe(10);
    }

    [Fact]
    public async Task BackTellsTheHostToReturnToTheList()
    {
        var screen = _harness.PickListScreen(Guid.NewGuid());
        var closed = false;
        screen.Closed += (_, _) => closed = true;

        screen.BackCommand.Execute(null);

        closed.ShouldBeTrue();
    }

    [Fact]
    public async Task ExportPdfSavesThePdfAndTellsWhereItWent()
    {
        var (screen, _, _) = await OpenAsync();

        await screen.ExportPdfCommand.ExecuteAsync(null);

        _harness.FileSave.Content.ShouldNotBeNull().Length.ShouldBeGreaterThan(100);
        screen.StatusMessage.ShouldBe(_harness.Localizer.Format("SalesPdf_Saved", @"C:\temp\document.pdf"));
    }

    [Fact]
    public async Task ExportingAnUnknownListShowsTheErrorAndSavesNothing()
    {
        var screen = _harness.PickListScreen(Guid.NewGuid());

        await screen.ExportPdfCommand.ExecuteAsync(null);

        screen.StatusMessage.ShouldBe(_harness.Localizer.Get("Error_picklist_not_found"));
        _harness.FileSave.Content.ShouldBeNull();
    }
}

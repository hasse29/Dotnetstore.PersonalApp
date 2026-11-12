using System.Globalization;
using Dotnetstore.PersonalApp.Modules.Products.Domain;
using Dotnetstore.PersonalApp.Modules.Products.Features.Products;
using Dotnetstore.PersonalApp.Modules.Products.Features.Stock;
using Dotnetstore.PersonalApp.Modules.Products.Features.StorageLocations;
using Dotnetstore.PersonalApp.Modules.Products.Resources;
using Dotnetstore.PersonalApp.Shared.Localization;

namespace Dotnetstore.PersonalApp.Modules.Products.Tests;

public sealed class StockViewModelTests : IDisposable
{
    private readonly ProductsFixture _fixture = new();
    private readonly Localizer _localizer = new();

    public StockViewModelTests()
    {
        _localizer.AddResources(ProductsStrings.ResourceManager);
        _localizer.SetCulture("en");
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public void Dispose() => _fixture.Dispose();

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        for (var i = 0; i < 500 && !condition(); i++)
        {
            await Task.Delay(10, Ct);
        }

        condition().ShouldBeTrue();
    }

    private string Error(string code) => _localizer.Get("Error_" + code);

    private async Task<ProductDto> ProductAsync(string number, decimal? minimum = null) =>
        (await _fixture.Create.HandleAsync(new CreateProduct.Command(ProductsFixture.Hardware(number) with { Name = "Item " + number, MinimumStock = minimum }), Ct)).Value;

    private async Task<StorageLocationDto> LocationAsync(string name, bool active = true) =>
        (await _fixture.CreateLocation.HandleAsync(new CreateStorageLocation.Command(new StorageLocationFields(name, null, active)), Ct)).Value;

    private StockViewModel Stock() =>
        new(
            _fixture.Overview,
            new ListStorageLocations.Handler(_fixture.DbFactory),
            _fixture.ListSuppliers,
            _fixture.Receive,
            _fixture.Issue,
            _fixture.Transfer,
            _localizer);

    private StockMovementEditorViewModel Editor(StockOverviewDto product, params StorageLocationDto[] locations) =>
        new(
            product,
            "pcs",
            [.. Enum.GetValues<StockMovementMode>().Select(m => new StockMovementModeOption(m, m.ToString()))],
            locations,
            [new SupplierOption(null, "None")],
            _fixture.Receive,
            _fixture.Issue,
            _fixture.Transfer,
            _localizer);

    private static StockOverviewDto Overview(ProductDto product) =>
        new(product.Id, product.Number, product.Name, product.Unit, 0, null);

    // ---- stock overview ----

    [Fact]
    public async Task TheOverviewListsProductsWithTotalsMinimumsAndLowStatus()
    {
        var low = await ProductAsync("HW-1", minimum: 5);
        var fine = await ProductAsync("HW-2", minimum: 2);
        var shelf = await LocationAsync("Shelf");
        await _fixture.Receive.HandleAsync(new ReceiveStock.Command(low.Id, shelf.Id, 3, null, null), Ct);
        await _fixture.Receive.HandleAsync(new ReceiveStock.Command(fine.Id, shelf.Id, 10, null, null), Ct);
        var stock = Stock();

        await stock.LoadCommand.ExecuteAsync(null);

        var lowRow = stock.Rows.Single(r => r.Item.ProductId == low.Id);
        lowRow.TotalText.ShouldBe($"3 {_localizer.Get("ProductUnit_Piece")}");
        lowRow.MinimumText.ShouldBe("5");
        lowRow.StatusText.ShouldBe(_localizer.Get("Stock_Low"));
        stock.Rows.Single(r => r.Item.ProductId == fine.Id).StatusText.ShouldBeEmpty();
        stock.StatusMessage.ShouldBe(_localizer.Format("Stock_Count", 2));
    }

    [Fact]
    public async Task ProductsWithoutAMinimumShowADashAndAreNeverLow()
    {
        await ProductAsync("HW-1");
        var stock = Stock();

        await stock.LoadCommand.ExecuteAsync(null);

        stock.Rows.Single().MinimumText.ShouldBe("-");
        stock.Rows.Single().StatusText.ShouldBeEmpty();
    }

    [Fact]
    public async Task LowOnlyShowsJustTheLowProducts()
    {
        var low = await ProductAsync("HW-1", minimum: 5);
        var fine = await ProductAsync("HW-2", minimum: 2);
        var shelf = await LocationAsync("Shelf");
        await _fixture.Receive.HandleAsync(new ReceiveStock.Command(low.Id, shelf.Id, 3, null, null), Ct);
        await _fixture.Receive.HandleAsync(new ReceiveStock.Command(fine.Id, shelf.Id, 10, null, null), Ct);
        var stock = Stock();
        await stock.LoadCommand.ExecuteAsync(null);

        stock.LowOnly = true;

        await WaitUntilAsync(() => stock.Rows.Count == 1);
        stock.Rows.Single().Item.ProductId.ShouldBe(low.Id);
    }

    [Fact]
    public async Task SearchNarrowsTheOverviewAndResetsToPageOne()
    {
        await ProductAsync("HW-1");
        await ProductAsync("HW-2");
        var stock = Stock();
        await stock.LoadCommand.ExecuteAsync(null);

        stock.Search = "HW-2";
        await stock.SearchCommand.ExecuteAsync(null);

        stock.Rows.Single().Item.Number.ShouldBe("HW-2");
        stock.CurrentPage.ShouldBe(1);
    }

    [Fact]
    public async Task ManyProductsArePagedAndThePagerWalksBothWays()
    {
        for (var i = 1; i <= 17; i++)
        {
            await ProductAsync($"HW-{i:00}");
        }

        var stock = Stock();
        await stock.LoadCommand.ExecuteAsync(null);

        stock.PageCount.ShouldBe(2);
        stock.ShowPager.ShouldBeTrue();
        stock.PageLabel.ShouldBe(_localizer.Format("Stock_PageOf", 1, 2));
        stock.PreviousPageCommand.CanExecute(null).ShouldBeFalse();

        await stock.NextPageCommand.ExecuteAsync(null);
        stock.Rows.Count.ShouldBe(2);
        stock.NextPageCommand.CanExecute(null).ShouldBeFalse();

        await stock.PreviousPageCommand.ExecuteAsync(null);
        stock.CurrentPage.ShouldBe(1);
    }

    [Fact]
    public async Task SelectingARowWithoutAnyLocationExplainsWhyThereIsNoEditor()
    {
        await ProductAsync("HW-1");
        var stock = Stock();
        await stock.LoadCommand.ExecuteAsync(null);

        stock.SelectedRow = stock.Rows.Single();

        await WaitUntilAsync(() => stock.StatusMessage == _localizer.Get("Stock_NoLocations"));
        stock.Editor.ShouldBeNull();
    }

    [Fact]
    public async Task SelectingARowOpensAnEditorWithLocationsAndOnlyActiveSuppliers()
    {
        await ProductAsync("HW-1");
        await LocationAsync("Shelf A");
        await LocationAsync("Shelf B");
        var stock = Stock();
        await stock.LoadCommand.ExecuteAsync(null);

        stock.SelectedRow = stock.Rows.Single();

        await WaitUntilAsync(() => stock.Editor is not null);
        stock.Editor!.Locations.Count.ShouldBe(2);
        stock.Editor!.Modes.Count.ShouldBe(3);
        stock.Editor!.Suppliers.Single().Name.ShouldBe(_localizer.Get("StockEditor_NoSupplier"));
        stock.Editor!.Title.ShouldContain("HW-1");
    }

    [Fact]
    public async Task SavingFromTheEditorClosesItReloadsAndReportsWhatWasRegistered()
    {
        var product = await ProductAsync("HW-1");
        await LocationAsync("Shelf A");
        var stock = Stock();
        await stock.LoadCommand.ExecuteAsync(null);
        stock.SelectedRow = stock.Rows.Single();
        await WaitUntilAsync(() => stock.Editor is not null);
        var editor = stock.Editor!;
        editor.Quantity = 7;

        await editor.SaveCommand.ExecuteAsync(null);

        await WaitUntilAsync(() => stock.Editor is null && stock.Rows.Single().Item.Total == 7);
        stock.StatusMessage.ShouldBe(_localizer.Format("Stock_Registered", _localizer.Get("StockMode_Receipt"), editor.Title));
        product.Id.ShouldBe(stock.Rows.Single().Item.ProductId);
    }

    [Fact]
    public async Task CancellingTheEditorClosesItAndClearsTheSelection()
    {
        await ProductAsync("HW-1");
        await LocationAsync("Shelf A");
        var stock = Stock();
        await stock.LoadCommand.ExecuteAsync(null);
        stock.SelectedRow = stock.Rows.Single();
        await WaitUntilAsync(() => stock.Editor is not null);

        stock.Editor!.CancelCommand.Execute(null);

        stock.Editor.ShouldBeNull();
        stock.SelectedRow.ShouldBeNull();
    }

    // ---- movement editor ----

    [Fact]
    public async Task TheEditorStartsAsAReceiptAtTheFirstLocation()
    {
        var product = await ProductAsync("HW-1");
        var a = await LocationAsync("Shelf A");
        var b = await LocationAsync("Shelf B");

        var editor = Editor(Overview(product), a, b);

        editor.IsReceipt.ShouldBeTrue();
        editor.IsTransfer.ShouldBeFalse();
        editor.SelectedLocation.ShouldBe(a);
        editor.SelectedToLocation.ShouldBe(b);
        editor.LocationLabel.ShouldBe(_localizer.Get("StockEditor_Location"));
    }

    [Fact]
    public async Task SwitchingToTransferRelabelsTheLocationAndShowsTheTarget()
    {
        var product = await ProductAsync("HW-1");
        var editor = Editor(Overview(product), await LocationAsync("Shelf A"), await LocationAsync("Shelf B"));

        editor.SelectedMode = editor.Modes.Single(m => m.Mode == StockMovementMode.Transfer);

        editor.IsTransfer.ShouldBeTrue();
        editor.IsReceipt.ShouldBeFalse();
        editor.LocationLabel.ShouldBe(_localizer.Get("StockEditor_FromLocation"));
    }

    [Fact]
    public async Task AnIssueOfMoreThanIsInStockIsRejected()
    {
        var product = await ProductAsync("HW-1");
        var shelf = await LocationAsync("Shelf A");
        await _fixture.Receive.HandleAsync(new ReceiveStock.Command(product.Id, shelf.Id, 2, null, null), Ct);
        var editor = Editor(Overview(product), shelf);
        editor.SelectedMode = editor.Modes.Single(m => m.Mode == StockMovementMode.Issue);
        editor.Quantity = 5;
        var raised = false;
        editor.Saved += (_, _) => raised = true;

        await editor.SaveCommand.ExecuteAsync(null);

        editor.ErrorMessage.ShouldBe(Error("stock_insufficient"));
        raised.ShouldBeFalse();
    }

    [Fact]
    public async Task AnIssueWithinStockSucceeds()
    {
        var product = await ProductAsync("HW-1");
        var shelf = await LocationAsync("Shelf A");
        await _fixture.Receive.HandleAsync(new ReceiveStock.Command(product.Id, shelf.Id, 5, null, null), Ct);
        var editor = Editor(Overview(product), shelf);
        editor.SelectedMode = editor.Modes.Single(m => m.Mode == StockMovementMode.Issue);
        editor.Quantity = 2;
        string? message = null;
        editor.Saved += (_, text) => message = text;

        await editor.SaveCommand.ExecuteAsync(null);

        message.ShouldNotBeNull();
        (await _fixture.ProductStock.HandleAsync(new GetProductStock.Query(product.Id), Ct)).Total.ShouldBe(3);
    }

    [Fact]
    public async Task ATransferMovesStockBetweenLocationsAndTheSameLocationIsRejected()
    {
        var product = await ProductAsync("HW-1");
        var a = await LocationAsync("Shelf A");
        var b = await LocationAsync("Shelf B");
        await _fixture.Receive.HandleAsync(new ReceiveStock.Command(product.Id, a.Id, 5, null, null), Ct);
        var editor = Editor(Overview(product), a, b);
        editor.SelectedMode = editor.Modes.Single(m => m.Mode == StockMovementMode.Transfer);
        editor.Quantity = 3;
        editor.SelectedToLocation = a;

        await editor.SaveCommand.ExecuteAsync(null);
        editor.ErrorMessage.ShouldBe(Error("stock_same_location"));

        editor.SelectedToLocation = b;
        await editor.SaveCommand.ExecuteAsync(null);
        editor.ErrorMessage.ShouldBeNull();
        var stock = await _fixture.ProductStock.HandleAsync(new GetProductStock.Query(product.Id), Ct);
        stock.Locations.Single(l => l.LocationName == "Shelf B").Quantity.ShouldBe(3);
    }

    [Fact]
    public async Task ARequiredFieldThatIsEmptyIsReportedBeforeAnythingIsSent()
    {
        var product = await ProductAsync("HW-1");
        var shelf = await LocationAsync("Shelf A");
        var editor = Editor(Overview(product), shelf);

        await editor.SaveCommand.ExecuteAsync(null);
        editor.ErrorMessage.ShouldBe(Error("stock_quantity_invalid"));

        editor.Quantity = 1;
        editor.SelectedLocation = null;
        await editor.SaveCommand.ExecuteAsync(null);
        editor.ErrorMessage.ShouldBe(Error("storage_location_required"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public async Task ANonPositiveQuantityIsRejected(int quantity)
    {
        var product = await ProductAsync("HW-1");
        var shelf = await LocationAsync("Shelf A");
        var editor = Editor(Overview(product), shelf);
        editor.Quantity = quantity;

        await editor.SaveCommand.ExecuteAsync(null);

        editor.ErrorMessage.ShouldBe(Error("stock_quantity_invalid"));
    }

    [Fact]
    public async Task ReceivingIntoAnInactiveLocationIsRejected()
    {
        var product = await ProductAsync("HW-1");
        var closed = await LocationAsync("Closed shelf", active: false);
        var editor = Editor(Overview(product), closed);
        editor.Quantity = 1;

        await editor.SaveCommand.ExecuteAsync(null);

        editor.ErrorMessage.ShouldBe(Error("storage_location_inactive"));
    }

    // ---- movement history ----

    private StockMovementsViewModel Movements() => new(_fixture.Movements, _localizer);

    [Fact]
    public async Task TheHistoryListsMovementsNewestFirstWithSignedQuantities()
    {
        var product = await ProductAsync("HW-1");
        var shelf = await LocationAsync("Shelf A");
        await _fixture.Receive.HandleAsync(new ReceiveStock.Command(product.Id, shelf.Id, 5, null, "Delivery"), Ct);
        await _fixture.Issue.HandleAsync(new IssueStock.Command(product.Id, shelf.Id, 2, null), Ct);
        var movements = Movements();

        await movements.LoadCommand.ExecuteAsync(null);

        movements.Movements.Count.ShouldBe(2);
        movements.FilterTypes.Count.ShouldBe(Enum.GetValues<StockMovementType>().Length + 1);
        movements.Movements.Single(m => m.Movement.Type == StockMovementType.Receipt).QuantityText.ShouldBe(string.Create(CultureInfo.CurrentCulture, $"{5:+0.##;-0.##;0}"));
        movements.Movements.Single(m => m.Movement.Type == StockMovementType.Issue).QuantityText.ShouldBe(string.Create(CultureInfo.CurrentCulture, $"{-2:+0.##;-0.##;0}"));
        movements.Movements.Single(m => m.Movement.Type == StockMovementType.Receipt).DetailText.ShouldBe("Delivery");
        movements.Movements.Single(m => m.Movement.Type == StockMovementType.Receipt).TypeText.ShouldBe(_localizer.Get("StockMovementType_Receipt"));
        movements.StatusMessage.ShouldBe(_localizer.Format("StockMovements_Count", 2));
        movements.ShowPager.ShouldBeFalse();
    }

    [Fact]
    public async Task FilteringTheHistoryByTypeAndSearching()
    {
        var product = await ProductAsync("HW-1");
        var other = await ProductAsync("HW-2");
        var shelf = await LocationAsync("Shelf A");
        await _fixture.Receive.HandleAsync(new ReceiveStock.Command(product.Id, shelf.Id, 5, null, null), Ct);
        await _fixture.Receive.HandleAsync(new ReceiveStock.Command(other.Id, shelf.Id, 4, null, null), Ct);
        await _fixture.Issue.HandleAsync(new IssueStock.Command(product.Id, shelf.Id, 2, null), Ct);
        var movements = Movements();
        await movements.LoadCommand.ExecuteAsync(null);

        movements.SelectedFilter = movements.FilterTypes.Single(t => t.Type == StockMovementType.Issue);
        await WaitUntilAsync(() => movements.Movements.Count == 1);

        movements.SelectedFilter = movements.FilterTypes[0];
        await WaitUntilAsync(() => movements.Movements.Count == 3);
        movements.Search = "HW-2";
        await movements.SearchCommand.ExecuteAsync(null);
        movements.Movements.Single().Movement.Type.ShouldBe(StockMovementType.Receipt);
    }

    [Fact]
    public async Task TheHistoryIsPaged()
    {
        var product = await ProductAsync("HW-1");
        var shelf = await LocationAsync("Shelf A");
        for (var i = 0; i < 17; i++)
        {
            await _fixture.Receive.HandleAsync(new ReceiveStock.Command(product.Id, shelf.Id, 1, null, null), Ct);
        }

        var movements = Movements();
        await movements.LoadCommand.ExecuteAsync(null);

        movements.PageCount.ShouldBe(2);
        movements.ShowPager.ShouldBeTrue();
        movements.Movements.Count.ShouldBe(15);
        movements.PageLabel.ShouldBe(_localizer.Format("StockMovements_PageOf", 1, 2));
        movements.PreviousPageCommand.CanExecute(null).ShouldBeFalse();

        await movements.NextPageCommand.ExecuteAsync(null);
        movements.Movements.Count.ShouldBe(2);
        movements.NextPageCommand.CanExecute(null).ShouldBeFalse();

        await movements.PreviousPageCommand.ExecuteAsync(null);
        movements.CurrentPage.ShouldBe(1);
    }
}

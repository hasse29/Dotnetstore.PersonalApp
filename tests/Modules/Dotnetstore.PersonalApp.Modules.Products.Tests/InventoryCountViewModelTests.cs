using Dotnetstore.PersonalApp.Modules.Products.Features.InventoryCounts;
using Dotnetstore.PersonalApp.Modules.Products.Features.Products;
using Dotnetstore.PersonalApp.Modules.Products.Features.Stock;
using Dotnetstore.PersonalApp.Modules.Products.Features.StorageLocations;
using Dotnetstore.PersonalApp.Modules.Products.Resources;
using Dotnetstore.PersonalApp.Shared.Localization;

namespace Dotnetstore.PersonalApp.Modules.Products.Tests;

public sealed class InventoryCountViewModelTests : IDisposable
{
    private readonly ProductsFixture _fixture = new();
    private readonly Localizer _localizer = new();

    public InventoryCountViewModelTests()
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

    private async Task<StorageLocationDto> LocationAsync(string name) =>
        (await _fixture.CreateLocation.HandleAsync(new CreateStorageLocation.Command(new StorageLocationFields(name, null, true)), Ct)).Value;

    private async Task<ProductDto> StockedProductAsync(string number, StorageLocationDto location, decimal quantity)
    {
        var product = (await _fixture.Create.HandleAsync(new CreateProduct.Command(ProductsFixture.Hardware(number) with { Name = "Item " + number }), Ct)).Value;
        await _fixture.Receive.HandleAsync(new ReceiveStock.Command(product.Id, location.Id, quantity, null, null), Ct);
        return product;
    }

    private InventoryCountsViewModel List() =>
        new(
            _fixture.ListCounts,
            _fixture.CreateCount,
            _fixture.DeleteCount,
            _fixture.GetCount,
            _fixture.SaveCount,
            _fixture.PostCount,
            new ListStorageLocations.Handler(_fixture.DbFactory),
            _fixture.Clock,
            _localizer);

    private InventoryCountEditorViewModel Editor(params CountLocationOption[] locations) =>
        new([new CountLocationOption(null, "All"), .. locations], _fixture.CreateCount, _localizer, _fixture.Clock.GetLocalNow());

    private async Task<InventoryCountDto> CountAsync(Guid? locationId = null, string name = "Stocktake") =>
        (await _fixture.CreateCount.HandleAsync(new CreateInventoryCount.Command(name, locationId, null, false), Ct)).Value;

    private InventoryCountScreenViewModel Screen(InventoryCountDto count) =>
        new(count, _fixture.GetCount, _fixture.SaveCount, _fixture.PostCount, _localizer);

    // ---- editor ----

    [Fact]
    public async Task TheEditorStartsWithADatedNameAndAllLocations()
    {
        var shelf = await LocationAsync("Shelf A");

        var editor = Editor(new CountLocationOption(shelf.Id, shelf.Name));

        editor.SelectedLocation!.Id.ShouldBeNull();
        editor.Name.ShouldBe(_localizer.Format("InventoryCountEditor_DefaultName", _fixture.Clock.GetLocalNow().ToString("d", System.Globalization.CultureInfo.CurrentCulture)));
        editor.CanIncludeUnstocked.ShouldBeFalse();
    }

    [Fact]
    public async Task IncludingUnstockedProductsIsOnlyPossibleForOneLocationAndResetsWhenGoingBackToAll()
    {
        var shelf = await LocationAsync("Shelf A");
        var editor = Editor(new CountLocationOption(shelf.Id, shelf.Name));

        editor.SelectedLocation = editor.Locations[1];
        editor.CanIncludeUnstocked.ShouldBeTrue();
        editor.IncludeUnstocked = true;

        editor.SelectedLocation = editor.Locations[0];
        editor.CanIncludeUnstocked.ShouldBeFalse();
        editor.IncludeUnstocked.ShouldBeFalse();
    }

    [Fact]
    public async Task SavingCreatesTheCountAndRaisesSaved()
    {
        var shelf = await LocationAsync("Shelf A");
        await StockedProductAsync("HW-1", shelf, 5);
        var editor = Editor();
        InventoryCountDto? saved = null;
        editor.Saved += (_, dto) => saved = dto;

        await editor.SaveCommand.ExecuteAsync(null);

        saved.ShouldNotBeNull().LineCount.ShouldBe(1);
        editor.ErrorMessage.ShouldBeNull();
    }

    [Fact]
    public async Task ABlankNameIsRejected()
    {
        var editor = Editor();
        editor.Name = "  ";
        var raised = false;
        editor.Saved += (_, _) => raised = true;

        await editor.SaveCommand.ExecuteAsync(null);

        editor.ErrorMessage.ShouldBe(Error("inventory_count_name_required"));
        raised.ShouldBeFalse();
    }

    [Fact]
    public async Task AnUnknownLocationIsRejected()
    {
        var editor = Editor(new CountLocationOption(Guid.NewGuid(), "Ghost"));
        editor.SelectedLocation = editor.Locations[1];

        await editor.SaveCommand.ExecuteAsync(null);

        editor.ErrorMessage.ShouldBe(Error("storage_location_not_found"));
    }

    [Fact]
    public async Task CancelRaisesCancelled()
    {
        var editor = Editor();
        var cancelled = false;
        editor.Cancelled += (_, _) => cancelled = true;

        editor.CancelCommand.Execute(null);

        cancelled.ShouldBeTrue();
    }

    // ---- screen ----

    [Fact]
    public async Task TheScreenShowsTheCountItsLocationAndEveryLineAsOpen()
    {
        var shelf = await LocationAsync("Shelf A");
        await StockedProductAsync("HW-1", shelf, 5);
        var count = await CountAsync(shelf.Id);
        var screen = Screen(count);

        await screen.LoadCommand.ExecuteAsync(null);

        screen.Heading.ShouldBe("Stocktake · Shelf A");
        screen.IsOpen.ShouldBeTrue();
        screen.IsEmpty.ShouldBeFalse();
        var line = screen.Lines.Single();
        line.IsEditable.ShouldBeTrue();
        line.CountedQuantity.ShouldBeNull();
        line.DifferenceText.ShouldBeEmpty();
        line.ExpectedText.ShouldBe($"5 {_localizer.Get("ProductUnit_Piece")}");
    }

    [Fact]
    public async Task ACountOfAllLocationsSaysSoInItsHeading()
    {
        var screen = Screen(await CountAsync());

        await screen.LoadCommand.ExecuteAsync(null);

        screen.Heading.ShouldBe($"Stocktake · {_localizer.Get("InventoryCounts_AllLocations")}");
        screen.IsEmpty.ShouldBeTrue();
    }

    [Fact]
    public async Task TypingACountedQuantityShowsTheDifference()
    {
        var shelf = await LocationAsync("Shelf A");
        await StockedProductAsync("HW-1", shelf, 5);
        var screen = Screen(await CountAsync(shelf.Id));
        await screen.LoadCommand.ExecuteAsync(null);
        var line = screen.Lines.Single();

        line.CountedQuantity = 3;
        line.DifferenceText.ShouldBe(string.Create(System.Globalization.CultureInfo.CurrentCulture, $"{-2:+0.##;-0.##;0}"));

        line.CountedQuantity = 5;
        line.DifferenceText.ShouldBe("0");

        line.CountedQuantity = 8;
        line.DifferenceText.ShouldBe(string.Create(System.Globalization.CultureInfo.CurrentCulture, $"{3:+0.##;-0.##;0}"));

        line.CountedQuantity = null;
        line.DifferenceText.ShouldBeEmpty();
    }

    [Fact]
    public async Task AnUnknownCountShowsTheError()
    {
        var count = await CountAsync();
        var screen = Screen(count with { Id = Guid.NewGuid() });

        await screen.LoadCommand.ExecuteAsync(null);

        screen.StatusMessage.ShouldBe(Error("inventory_count_not_found"));
        screen.Lines.ShouldBeEmpty();
    }

    [Fact]
    public async Task SavingKeepsTheCountedQuantitiesForNextTime()
    {
        var shelf = await LocationAsync("Shelf A");
        await StockedProductAsync("HW-1", shelf, 5);
        var count = await CountAsync(shelf.Id);
        var screen = Screen(count);
        await screen.LoadCommand.ExecuteAsync(null);
        screen.Lines.Single().CountedQuantity = 4;

        await screen.SaveCommand.ExecuteAsync(null);
        screen.StatusMessage.ShouldBe(_localizer.Get("InventoryCountScreen_Saved"));

        var reopened = Screen(count);
        await reopened.LoadCommand.ExecuteAsync(null);
        reopened.Lines.Single().CountedQuantity.ShouldBe(4);
    }

    [Fact]
    public async Task ANegativeCountedQuantityIsRejected()
    {
        var shelf = await LocationAsync("Shelf A");
        await StockedProductAsync("HW-1", shelf, 5);
        var screen = Screen(await CountAsync(shelf.Id));
        await screen.LoadCommand.ExecuteAsync(null);
        screen.Lines.Single().CountedQuantity = -1;

        await screen.SaveCommand.ExecuteAsync(null);

        screen.StatusMessage.ShouldBe(Error("stock_counted_invalid"));
    }

    [Fact]
    public async Task PostingNeedsConfirmationThatCanBeWithdrawn()
    {
        var screen = Screen(await CountAsync());

        screen.RequestPostCommand.Execute(null);
        screen.IsConfirmingPost.ShouldBeTrue();
        screen.CancelPostCommand.Execute(null);

        screen.IsConfirmingPost.ShouldBeFalse();
    }

    [Fact]
    public async Task PostingSavesWhatIsOnScreenAdjustsTheStockAndLocksTheCount()
    {
        var shelf = await LocationAsync("Shelf A");
        var product = await StockedProductAsync("HW-1", shelf, 5);
        var screen = Screen(await CountAsync(shelf.Id));
        await screen.LoadCommand.ExecuteAsync(null);
        screen.Lines.Single().CountedQuantity = 3;
        screen.RequestPostCommand.Execute(null);

        await screen.ConfirmPostCommand.ExecuteAsync(null);

        screen.IsConfirmingPost.ShouldBeFalse();
        screen.IsOpen.ShouldBeFalse();
        screen.Lines.Single().IsEditable.ShouldBeFalse();
        screen.StatusMessage.ShouldNotBeNull().ShouldNotBe(_localizer.Get("InventoryCountScreen_Saved"));
        (await _fixture.ProductStock.HandleAsync(new GetProductStock.Query(product.Id), Ct)).Total.ShouldBe(3);
    }

    [Fact]
    public async Task APostedCountCannotBePostedOrSavedAgain()
    {
        var shelf = await LocationAsync("Shelf A");
        await StockedProductAsync("HW-1", shelf, 5);
        var screen = Screen(await CountAsync(shelf.Id));
        await screen.LoadCommand.ExecuteAsync(null);
        screen.Lines.Single().CountedQuantity = 5;
        await screen.ConfirmPostCommand.ExecuteAsync(null);

        await screen.ConfirmPostCommand.ExecuteAsync(null);

        screen.StatusMessage.ShouldBe(Error("inventory_count_posted"));
    }

    [Fact]
    public async Task BackRaisesClosed()
    {
        var screen = Screen(await CountAsync());
        var closed = false;
        screen.Closed += (_, _) => closed = true;

        screen.BackCommand.Execute(null);

        closed.ShouldBeTrue();
    }

    // ---- list ----

    [Fact]
    public async Task TheListShowsEachCountWithItsStatusLocationAndProgress()
    {
        var shelf = await LocationAsync("Shelf A");
        await StockedProductAsync("HW-1", shelf, 5);
        await CountAsync(shelf.Id, "Shelf count");
        await CountAsync(null, "Full count");
        var list = List();

        await list.LoadCommand.ExecuteAsync(null);

        list.Counts.Count.ShouldBe(2);
        var row = list.Counts.Single(c => c.Count.Name == "Shelf count");
        row.LocationText.ShouldBe("Shelf A");
        row.ProgressText.ShouldBe("0 / 1");
        row.StatusText.ShouldBe(_localizer.Get("InventoryCounts_Open"));
        list.Counts.Single(c => c.Count.Name == "Full count").LocationText.ShouldBe(_localizer.Get("InventoryCounts_AllLocations"));
        list.StatusMessage.ShouldBe(_localizer.Format("InventoryCounts_Count", 2));
    }

    [Fact]
    public async Task NewOffersAllLocationsPlusTheActiveOnes()
    {
        await LocationAsync("Shelf A");
        var list = List();

        await list.NewCommand.ExecuteAsync(null);

        list.Editor.ShouldNotBeNull().Locations.Select(l => l.Name).ShouldBe([_localizer.Get("InventoryCounts_AllLocations"), "Shelf A"]);
    }

    [Fact]
    public async Task SavingANewCountOpensItsScreenStraightAway()
    {
        var list = List();
        await list.NewCommand.ExecuteAsync(null);

        await list.Editor!.SaveCommand.ExecuteAsync(null);

        list.Editor.ShouldBeNull();
        list.Screen.ShouldNotBeNull();
        (await _fixture.ListCounts.HandleAsync(new ListInventoryCounts.Query(), Ct)).TotalCount.ShouldBe(1);
    }

    [Fact]
    public async Task CancellingTheEditorClosesIt()
    {
        var list = List();
        await list.NewCommand.ExecuteAsync(null);

        list.Editor!.CancelCommand.Execute(null);

        list.Editor.ShouldBeNull();
        list.Screen.ShouldBeNull();
    }

    [Fact]
    public async Task OpeningACountShowsItsScreenAndLeavingItReloadsTheList()
    {
        await CountAsync();
        var list = List();
        list.OpenCommand.CanExecute(null).ShouldBeFalse();
        await list.LoadCommand.ExecuteAsync(null);
        list.SelectedCount = list.Counts.Single();

        list.OpenCommand.Execute(null);
        list.Screen.ShouldNotBeNull();
        await WaitUntilAsync(() => list.Screen!.Heading.StartsWith("Stocktake", StringComparison.Ordinal));
        await CountAsync(null, "Another");

        list.Screen!.BackCommand.Execute(null);

        list.Screen.ShouldBeNull();
        await WaitUntilAsync(() => list.Counts.Count == 2);
    }

    [Fact]
    public async Task DeletingNeedsConfirmationAndHandlesACountThatIsAlreadyGone()
    {
        var gone = await CountAsync(null, "Gone");
        await CountAsync(null, "Kept");
        var list = List();
        list.RequestDeleteCommand.CanExecute(null).ShouldBeFalse();
        await list.LoadCommand.ExecuteAsync(null);
        list.SelectedCount = list.Counts.Single(c => c.Count.Id == gone.Id);

        list.RequestDeleteCommand.Execute(null);
        list.IsConfirmingDelete.ShouldBeTrue();
        list.CancelDeleteCommand.Execute(null);
        list.IsConfirmingDelete.ShouldBeFalse();

        await _fixture.DeleteCount.HandleAsync(new DeleteInventoryCount.Command(gone.Id), Ct);
        await list.ConfirmDeleteCommand.ExecuteAsync(null);
        list.StatusMessage.ShouldBe(Error("inventory_count_not_found"));

        list.SelectedCount = list.Counts.Single();
        await list.ConfirmDeleteCommand.ExecuteAsync(null);
        list.Counts.ShouldBeEmpty();
    }
}

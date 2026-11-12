using System.Globalization;
using Dotnetstore.PersonalApp.Modules.Products.Domain;
using Dotnetstore.PersonalApp.Modules.Products.Features.ProductCategories;
using Dotnetstore.PersonalApp.Modules.Products.Features.Products;
using Dotnetstore.PersonalApp.Modules.Products.Features.Stock;
using Dotnetstore.PersonalApp.Modules.Products.Features.StorageLocations;
using Dotnetstore.PersonalApp.Modules.Products.Features.SupplierPrices;
using Dotnetstore.PersonalApp.Modules.Products.Features.Suppliers;
using Dotnetstore.PersonalApp.Modules.Products.Resources;
using Dotnetstore.PersonalApp.Shared.Localization;

namespace Dotnetstore.PersonalApp.Modules.Products.Tests;

public sealed class ProductsListAndPanelsViewModelTests : IDisposable
{
    private readonly ProductsFixture _fixture = new();
    private readonly Localizer _localizer = new();

    public ProductsListAndPanelsViewModelTests()
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

    private ProductsViewModel List(bool withStock = false) =>
        new(
            _fixture.List,
            _fixture.Create,
            _fixture.Update,
            _fixture.Delete,
            new ListProductCategories.Handler(_fixture.DbFactory),
            _localizer,
            null,
            null,
            withStock ? _fixture.ProductStock : null);

    private async Task<ProductDto> AddAsync(string number, ProductKind kind = ProductKind.Hardware, ProductUnit unit = ProductUnit.Piece, decimal? minimumStock = null)
    {
        var fields = ProductsFixture.Hardware(number) with { Name = "Item " + number, Kind = kind, Unit = unit, MinimumStock = minimumStock };
        return (await _fixture.Create.HandleAsync(new CreateProduct.Command(fields), Ct)).Value;
    }

    // ---- list ----

    [Fact]
    public async Task TheListLoadsWithAllKindsAndFormatsEachRow()
    {
        var product = await AddAsync("HW-1");
        var list = List();

        await list.LoadCommand.ExecuteAsync(null);

        list.SelectedFilter.ShouldNotBeNull().Kind.ShouldBeNull();
        list.FilterKinds.Count.ShouldBe(Enum.GetValues<ProductKind>().Length + 1);
        var row = list.Products.Single();
        row.Product.Id.ShouldBe(product.Id);
        row.KindLabel.ShouldBe(_localizer.Get("ProductKind_Hardware"));
        row.PriceText.ShouldBe(string.Create(CultureInfo.CurrentCulture, $"{product.UnitPrice:N2} / {_localizer.Get("ProductUnit_Piece")}"));
        list.StatusMessage.ShouldBe(_localizer.Format("Products_Count", 1));
        list.ShowPager.ShouldBeFalse();
    }

    [Fact]
    public async Task FilteringByKindShowsOnlyThatKindAndClosesTheEditor()
    {
        await AddAsync("HW-1");
        await AddAsync("SW-1", ProductKind.Software, ProductUnit.License);
        var list = List();
        await list.LoadCommand.ExecuteAsync(null);
        list.NewCommand.Execute(null);

        list.SelectedFilter = list.FilterKinds.Single(k => k.Kind == ProductKind.Software);

        await WaitUntilAsync(() => list.Products.Count == 1);
        list.Products.Single().Product.Number.ShouldBe("SW-1");
        list.Editor.ShouldBeNull();
    }

    [Fact]
    public async Task SearchReturnsToPageOneAndFilters()
    {
        await AddAsync("HW-1");
        await AddAsync("HW-2");
        var list = List();
        await list.LoadCommand.ExecuteAsync(null);

        list.Search = "HW-2";
        await list.SearchCommand.ExecuteAsync(null);

        list.Products.Single().Product.Number.ShouldBe("HW-2");
        list.CurrentPage.ShouldBe(1);
    }

    [Fact]
    public async Task ManyProductsArePagedAndThePagerWalksBothWays()
    {
        for (var i = 1; i <= 17; i++)
        {
            await AddAsync($"HW-{i:00}");
        }

        var list = List();
        await list.LoadCommand.ExecuteAsync(null);

        list.PageCount.ShouldBe(2);
        list.ShowPager.ShouldBeTrue();
        list.Products.Count.ShouldBe(15);
        list.PageLabel.ShouldBe(_localizer.Format("Products_PageOf", 1, 2));
        list.PreviousPageCommand.CanExecute(null).ShouldBeFalse();
        list.NextPageCommand.CanExecute(null).ShouldBeTrue();

        await list.NextPageCommand.ExecuteAsync(null);
        list.CurrentPage.ShouldBe(2);
        list.Products.Count.ShouldBe(2);
        list.NextPageCommand.CanExecute(null).ShouldBeFalse();

        await list.PreviousPageCommand.ExecuteAsync(null);
        list.CurrentPage.ShouldBe(1);
        list.Products.Count.ShouldBe(15);
    }

    [Fact]
    public async Task DeletingTheLastProductOnTheLastPageStepsBack()
    {
        for (var i = 1; i <= 16; i++)
        {
            await AddAsync($"HW-{i:00}");
        }

        var list = List();
        await list.LoadCommand.ExecuteAsync(null);
        await list.NextPageCommand.ExecuteAsync(null);
        list.SelectedProduct = list.Products.Single();
        list.RequestDeleteCommand.Execute(null);

        await list.ConfirmDeleteCommand.ExecuteAsync(null);

        list.CurrentPage.ShouldBe(1);
        list.PageCount.ShouldBe(1);
        list.Products.Count.ShouldBe(15);
    }

    [Fact]
    public async Task SelectingNewAndCancelOpenAndCloseTheEditor()
    {
        await AddAsync("HW-1");
        var list = List();
        await list.LoadCommand.ExecuteAsync(null);

        list.SelectedProduct = list.Products.Single();
        list.Editor.ShouldNotBeNull().Number.ShouldBe("HW-1");

        list.NewCommand.Execute(null);
        list.SelectedProduct.ShouldBeNull();
        list.Editor.ShouldNotBeNull().Number.ShouldBeEmpty();
        list.Editor.Categories.Count.ShouldBe(1);

        list.Editor.CancelCommand.Execute(null);
        list.Editor.ShouldBeNull();
    }

    [Fact]
    public async Task TheEditorOffersTheExistingCategories()
    {
        await _fixture.CreateCategory.HandleAsync(new CreateProductCategory.Command("Cables", null), Ct);
        var list = List();
        await list.LoadCommand.ExecuteAsync(null);

        list.NewCommand.Execute(null);

        list.Editor!.Categories.Select(c => c.Name).ShouldBe([_localizer.Get("Products_NoCategory"), "Cables"]);
    }

    [Fact]
    public async Task SavingFromTheEditorClosesItAndReloads()
    {
        var list = List();
        await list.LoadCommand.ExecuteAsync(null);
        list.NewCommand.Execute(null);
        list.Editor!.Number = "HW-9";
        list.Editor.Name = "Router";
        list.Editor.UnitPrice = 900;

        await list.Editor.SaveCommand.ExecuteAsync(null);

        await WaitUntilAsync(() => list.StatusMessage == _localizer.Format("Products_Saved", "Router"));
        list.Editor.ShouldBeNull();
        list.Products.Single().Product.Number.ShouldBe("HW-9");
    }

    [Fact]
    public async Task DeleteNeedsConfirmationAndHandlesAMissingProduct()
    {
        var gone = await AddAsync("HW-1");
        await AddAsync("HW-2");
        var list = List();
        list.RequestDeleteCommand.CanExecute(null).ShouldBeFalse();
        await list.LoadCommand.ExecuteAsync(null);
        list.SelectedProduct = list.Products.Single(p => p.Product.Id == gone.Id);

        list.RequestDeleteCommand.Execute(null);
        list.IsConfirmingDelete.ShouldBeTrue();
        list.CancelDeleteCommand.Execute(null);
        list.IsConfirmingDelete.ShouldBeFalse();

        await _fixture.Delete.HandleAsync(new DeleteProduct.Command(gone.Id), Ct);
        await list.ConfirmDeleteCommand.ExecuteAsync(null);
        list.StatusMessage.ShouldBe(Error("product_not_found"));

        list.SelectedProduct = list.Products.Single();
        await list.ConfirmDeleteCommand.ExecuteAsync(null);
        list.Products.ShouldBeEmpty();
    }

    [Fact]
    public async Task AProductWithStockCannotBeDeleted()
    {
        var product = await AddAsync("HW-1");
        var location = (await _fixture.CreateLocation.HandleAsync(new CreateStorageLocation.Command(new StorageLocationFields("Shelf", null, true)), Ct)).Value;
        await _fixture.Receive.HandleAsync(new ReceiveStock.Command(product.Id, location.Id, 5, null, null), Ct);
        var list = List();
        await list.LoadCommand.ExecuteAsync(null);
        list.SelectedProduct = list.Products.Single();

        await list.ConfirmDeleteCommand.ExecuteAsync(null);

        list.StatusMessage.ShouldBe(Error("product_in_use"));
        list.Products.Count.ShouldBe(1);
    }

    [Fact]
    public async Task DetailOpensWithTheRightPartsAndBackReloads()
    {
        var hardware = await AddAsync("HW-1");
        await AddAsync("CO-1", ProductKind.Consulting, ProductUnit.Hour);
        var list = List(withStock: true);
        list.OpenDetailCommand.CanExecute(null).ShouldBeFalse();
        await list.LoadCommand.ExecuteAsync(null);

        list.SelectedProduct = list.Products.Single(p => p.Product.Id == hardware.Id);
        list.OpenDetailCommand.Execute(null);
        list.Detail.ShouldNotBeNull().StockCard.ShouldNotBeNull();
        list.Detail.SupplierPriceList.ShouldBeNull();
        list.Detail.BackCommand.Execute(null);
        list.Detail.ShouldBeNull();

        list.SelectedProduct = list.Products.Single(p => p.Product.Number == "CO-1");
        list.OpenDetailCommand.Execute(null);
        list.Detail.ShouldNotBeNull().StockCard.ShouldBeNull();
    }

    [Fact]
    public async Task EditFromTheDetailReturnsToTheListWithTheProductSelectedInItsEditor()
    {
        var product = await AddAsync("HW-1");
        var list = List();
        await list.LoadCommand.ExecuteAsync(null);
        list.SelectedProduct = list.Products.Single();
        list.OpenDetailCommand.Execute(null);
        var detail = list.Detail!;

        detail.EditCommand.Execute(null);

        await WaitUntilAsync(() => list.Detail is null && list.SelectedProduct?.Product.Id == product.Id);
        list.Editor.ShouldNotBeNull().Number.ShouldBe("HW-1");
    }

    // ---- stock card ----

    private ProductStockViewModel StockCard(Guid productId) => new(productId, "pcs", _fixture.ProductStock, _localizer);

    private async Task<StorageLocationDto> LocationAsync(string name) =>
        (await _fixture.CreateLocation.HandleAsync(new CreateStorageLocation.Command(new StorageLocationFields(name, null, true)), Ct)).Value;

    [Fact]
    public async Task AProductWithoutStockShowsAnEmptyCard()
    {
        var product = await AddAsync("HW-1");
        var card = StockCard(product.Id);

        await card.LoadCommand.ExecuteAsync(null);

        card.IsEmpty.ShouldBeTrue();
        card.TotalText.ShouldBe(string.Create(CultureInfo.CurrentCulture, $"{0:0.##} pcs"));
        card.MinimumText.ShouldBe("-");
        card.WarningText.ShouldBeNull();
    }

    [Fact]
    public async Task StockIsListedPerLocationWithTheTotal()
    {
        var product = await AddAsync("HW-1", minimumStock: 5);
        var a = await LocationAsync("Shelf A");
        var b = await LocationAsync("Shelf B");
        await _fixture.Receive.HandleAsync(new ReceiveStock.Command(product.Id, a.Id, 8, null, null), Ct);
        await _fixture.Receive.HandleAsync(new ReceiveStock.Command(product.Id, b.Id, 4, null, null), Ct);
        var card = StockCard(product.Id);

        await card.LoadCommand.ExecuteAsync(null);

        card.IsEmpty.ShouldBeFalse();
        card.Locations.Count.ShouldBe(2);
        card.Locations.Single(l => l.LocationName == "Shelf A").QuantityText.ShouldBe("8 pcs");
        card.TotalText.ShouldBe("12 pcs");
        card.MinimumText.ShouldBe("5 pcs");
        card.WarningText.ShouldBeNull();
    }

    [Fact]
    public async Task StockAtOrBelowTheMinimumShowsAWarning()
    {
        var product = await AddAsync("HW-1", minimumStock: 5);
        var shelf = await LocationAsync("Shelf A");
        await _fixture.Receive.HandleAsync(new ReceiveStock.Command(product.Id, shelf.Id, 5, null, null), Ct);
        var card = StockCard(product.Id);

        await card.LoadCommand.ExecuteAsync(null);

        card.WarningText.ShouldBe(_localizer.Get("ProductStock_Low"));
    }

    // ---- supplier prices ----

    private SupplierPricesViewModel Prices(Guid productId) =>
        new(productId, "pcs", _fixture.ListPrices, _fixture.SetPrice, _fixture.DeletePrice, _fixture.ListSuppliers, _localizer);

    private async Task<SupplierDto> SupplierAsync(string name, bool active = true) =>
        (await _fixture.CreateSupplier.HandleAsync(new CreateSupplier.Command(new SupplierFields(name, null, null, null, null, null, active)), Ct)).Value;

    [Fact]
    public async Task WithoutPricesTheListIsEmpty()
    {
        var product = await AddAsync("HW-1");
        var prices = Prices(product.Id);

        await prices.LoadCommand.ExecuteAsync(null);

        prices.IsEmpty.ShouldBeTrue();
        prices.IsEditing.ShouldBeFalse();
        prices.IsNotEditing.ShouldBeTrue();
    }

    [Fact]
    public async Task AddingWithoutAnySupplierSaysSoAndOpensNoForm()
    {
        var product = await AddAsync("HW-1");
        var prices = Prices(product.Id);
        await prices.LoadCommand.ExecuteAsync(null);

        await prices.AddCommand.ExecuteAsync(null);

        prices.IsEditing.ShouldBeFalse();
        prices.StatusMessage.ShouldBe(_localizer.Get("SupplierPrices_NoSuppliers"));
    }

    [Fact]
    public async Task OnlyActiveSuppliersWithoutAPriceCanBeAdded()
    {
        var product = await AddAsync("HW-1");
        var acme = await SupplierAsync("Acme");
        await SupplierAsync("Dormant", active: false);
        var prices = Prices(product.Id);
        await prices.LoadCommand.ExecuteAsync(null);

        await prices.AddCommand.ExecuteAsync(null);
        prices.IsEditing.ShouldBeTrue();
        prices.AvailableSuppliers.Select(s => s.Name).ShouldBe(["Acme"]);
        prices.SelectedSupplier!.Id.ShouldBe(acme.Id);
        prices.IsSupplierLocked.ShouldBeFalse();
        prices.Price = 10;
        await prices.SaveCommand.ExecuteAsync(null);

        await prices.AddCommand.ExecuteAsync(null);
        prices.IsEditing.ShouldBeFalse();
        prices.StatusMessage.ShouldBe(_localizer.Get("SupplierPrices_AllPriced"));
    }

    [Fact]
    public async Task SavingAPriceListsItAndSaysWhoseItWas()
    {
        var product = await AddAsync("HW-1");
        await SupplierAsync("Acme");
        var prices = Prices(product.Id);
        await prices.LoadCommand.ExecuteAsync(null);
        await prices.AddCommand.ExecuteAsync(null);
        prices.ArticleNumber = "A-100";
        prices.Price = 12.5m;

        await prices.SaveCommand.ExecuteAsync(null);

        prices.IsEditing.ShouldBeFalse();
        prices.StatusMessage.ShouldBe(_localizer.Format("SupplierPrices_Saved", "Acme"));
        var row = prices.Prices.Single();
        row.PriceText.ShouldBe(string.Create(CultureInfo.CurrentCulture, $"{12.5m:N2} / pcs"));
        row.DetailText.ShouldStartWith("A-100");
        row.BadgeText.ShouldBeEmpty();
        prices.IsEmpty.ShouldBeFalse();
    }

    [Fact]
    public async Task TheCheapestOfSeveralPricesIsMarkedLowest()
    {
        var product = await AddAsync("HW-1");
        var acme = await SupplierAsync("Acme");
        var beta = await SupplierAsync("Beta");
        await _fixture.SetPrice.HandleAsync(new SetSupplierPrice.Command(product.Id, acme.Id, null, 20), Ct);
        await _fixture.SetPrice.HandleAsync(new SetSupplierPrice.Command(product.Id, beta.Id, null, 15), Ct);
        var prices = Prices(product.Id);

        await prices.LoadCommand.ExecuteAsync(null);

        prices.Prices.Single(p => p.Price.SupplierId == beta.Id).BadgeText.ShouldBe(_localizer.Get("SupplierPrices_Lowest"));
        prices.Prices.Single(p => p.Price.SupplierId == acme.Id).BadgeText.ShouldBeEmpty();
    }

    [Fact]
    public async Task APriceWithoutAValueOrASupplierIsRejected()
    {
        var product = await AddAsync("HW-1");
        await SupplierAsync("Acme");
        var prices = Prices(product.Id);
        await prices.LoadCommand.ExecuteAsync(null);
        await prices.AddCommand.ExecuteAsync(null);

        await prices.SaveCommand.ExecuteAsync(null);
        prices.ErrorMessage.ShouldBe(Error("supplier_price_required"));
        prices.IsEditing.ShouldBeTrue();

        prices.SelectedSupplier = null;
        prices.Price = 5;
        await prices.SaveCommand.ExecuteAsync(null);
        prices.ErrorMessage.ShouldBe(Error("supplier_required"));
    }

    [Fact]
    public async Task ANegativePriceIsRejectedByTheHandler()
    {
        var product = await AddAsync("HW-1");
        await SupplierAsync("Acme");
        var prices = Prices(product.Id);
        await prices.LoadCommand.ExecuteAsync(null);
        await prices.AddCommand.ExecuteAsync(null);
        prices.Price = -1;

        await prices.SaveCommand.ExecuteAsync(null);

        prices.ErrorMessage.ShouldBe(Error("supplier_price_invalid"));
        prices.Prices.ShouldBeEmpty();
    }

    [Fact]
    public async Task EditingAPriceLocksTheSupplierAndCancelClosesTheForm()
    {
        var product = await AddAsync("HW-1");
        var acme = await SupplierAsync("Acme");
        await _fixture.SetPrice.HandleAsync(new SetSupplierPrice.Command(product.Id, acme.Id, "A-1", 20), Ct);
        var prices = Prices(product.Id);
        await prices.LoadCommand.ExecuteAsync(null);
        prices.EditCommand.CanExecute(null).ShouldBeFalse();
        prices.SelectedPrice = prices.Prices.Single();

        prices.EditCommand.Execute(null);
        prices.IsEditing.ShouldBeTrue();
        prices.IsSupplierLocked.ShouldBeTrue();
        prices.ArticleNumber.ShouldBe("A-1");
        prices.Price.ShouldBe(20);

        prices.Price = 18;
        await prices.SaveCommand.ExecuteAsync(null);
        prices.Prices.Single().Price.Price.ShouldBe(18);

        prices.SelectedPrice = prices.Prices.Single();
        prices.EditCommand.Execute(null);
        prices.CancelCommand.Execute(null);
        prices.IsEditing.ShouldBeFalse();
    }

    [Fact]
    public async Task RemovingAPriceNeedsConfirmationAndCanBeWithdrawn()
    {
        var product = await AddAsync("HW-1");
        var acme = await SupplierAsync("Acme");
        await _fixture.SetPrice.HandleAsync(new SetSupplierPrice.Command(product.Id, acme.Id, null, 20), Ct);
        var prices = Prices(product.Id);
        await prices.LoadCommand.ExecuteAsync(null);
        prices.SelectedPrice = prices.Prices.Single();

        prices.RequestDeleteCommand.Execute(null);
        prices.IsConfirmingDelete.ShouldBeTrue();
        prices.CancelDeleteCommand.Execute(null);
        prices.IsConfirmingDelete.ShouldBeFalse();
        prices.Prices.Count.ShouldBe(1);

        prices.RequestDeleteCommand.Execute(null);
        await prices.ConfirmDeleteCommand.ExecuteAsync(null);
        prices.Prices.ShouldBeEmpty();
        prices.IsConfirmingDelete.ShouldBeFalse();
        prices.StatusMessage.ShouldBeNull();
    }

    [Fact]
    public async Task RemovingAPriceThatIsAlreadyGoneShowsTheError()
    {
        var product = await AddAsync("HW-1");
        var acme = await SupplierAsync("Acme");
        var price = (await _fixture.SetPrice.HandleAsync(new SetSupplierPrice.Command(product.Id, acme.Id, null, 20), Ct)).Value;
        var prices = Prices(product.Id);
        await prices.LoadCommand.ExecuteAsync(null);
        prices.SelectedPrice = prices.Prices.Single();
        await _fixture.DeletePrice.HandleAsync(new DeleteSupplierPrice.Command(price.Id), Ct);

        await prices.ConfirmDeleteCommand.ExecuteAsync(null);

        prices.StatusMessage.ShouldBe(Error("supplier_price_not_found"));
    }
}

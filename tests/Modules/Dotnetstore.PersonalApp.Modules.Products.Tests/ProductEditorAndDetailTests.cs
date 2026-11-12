using System.Globalization;
using Dotnetstore.PersonalApp.Modules.Products.Domain;
using Dotnetstore.PersonalApp.Modules.Products.Features.Products;
using Dotnetstore.PersonalApp.Modules.Products.Resources;
using Dotnetstore.PersonalApp.Shared.Localization;

namespace Dotnetstore.PersonalApp.Modules.Products.Tests;

public sealed class ProductEditorAndDetailTests : IDisposable
{
    private readonly ProductsFixture _fixture = new();
    private readonly Localizer _localizer = new();
    private readonly Guid _categoryId = Guid.NewGuid();

    public ProductEditorAndDetailTests()
    {
        _localizer.AddResources(ProductsStrings.ResourceManager);
        _localizer.SetCulture("en");
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public void Dispose() => _fixture.Dispose();

    private string Error(string code) => _localizer.Get("Error_" + code);

    private static ProductKindOption[] Kinds => [.. Enum.GetValues<ProductKind>().Select(k => new ProductKindOption(k, k.ToString()))];

    private static ProductUnitOption[] Units => [.. Enum.GetValues<ProductUnit>().Select(u => new ProductUnitOption(u, u.ToString()))];

    private ProductCategoryOption[] Categories(Guid? id = null) =>
        [new ProductCategoryOption(null, "None"), new ProductCategoryOption(id ?? _categoryId, "Cables")];

    private ProductEditorViewModel Editor(ProductDto? existing = null, Guid? categoryId = null) =>
        new(existing, Kinds, Units, Categories(categoryId), _fixture.Create, _fixture.Update, _localizer);

    private async Task<ProductDto> AddAsync(string number = "HW-1") =>
        (await _fixture.Create.HandleAsync(new CreateProduct.Command(ProductsFixture.Hardware(number)), Ct)).Value;

    private static void Fill(ProductEditorViewModel editor, string number = "HW-1")
    {
        editor.Number = number;
        editor.Name = "Cat6 cable";
        editor.UnitPrice = 50;
    }

    // ---- editor ----

    [Fact]
    public async Task ANewEditorStartsAsHardwareInPiecesWithDefaultVat()
    {
        var editor = Editor();

        editor.Title.ShouldBe(_localizer.Get("ProductEditor_New"));
        editor.IsHardware.ShouldBeTrue();
        editor.IsStocked.ShouldBeTrue();
        editor.SelectedUnit!.Unit.ShouldBe(ProductUnit.Piece);
        editor.VatRate.ShouldBe(25);
        editor.UnitPrice.ShouldBe(0);
        editor.SelectedCategory!.Id.ShouldBeNull();
    }

    [Theory]
    [InlineData((int)ProductKind.Software, (int)ProductUnit.License, false, true, false)]
    [InlineData((int)ProductKind.Consulting, (int)ProductUnit.Hour, false, false, true)]
    [InlineData((int)ProductKind.Hardware, (int)ProductUnit.Piece, true, false, false)]
    public async Task ChangingTheKindOfANewProductPicksTheUsualUnit(int kindValue, int unitValue, bool hardware, bool software, bool consulting)
    {
        var editor = Editor();

        editor.SelectedKind = editor.Kinds.Single(k => k.Kind == (ProductKind)kindValue);

        editor.SelectedUnit!.Unit.ShouldBe((ProductUnit)unitValue);
        editor.IsHardware.ShouldBe(hardware);
        editor.IsSoftware.ShouldBe(software);
        editor.IsConsulting.ShouldBe(consulting);
        editor.IsStocked.ShouldBe(!consulting);
    }

    [Fact]
    public async Task ChangingTheKindOfAnExistingProductKeepsItsSavedUnit()
    {
        var editor = Editor(await AddAsync());

        editor.SelectedKind = editor.Kinds.Single(k => k.Kind == ProductKind.Consulting);

        editor.SelectedUnit!.Unit.ShouldBe(ProductUnit.Piece);
    }

    [Fact]
    public async Task AnExistingProductFillsTheEditor()
    {
        var product = await AddAsync();

        var editor = Editor(product);

        editor.Title.ShouldBe(_localizer.Get("ProductEditor_Edit"));
        editor.Number.ShouldBe("HW-1");
        editor.Name.ShouldBe("Cat6 cable 2m");
        editor.Manufacturer.ShouldBe("Acme");
        editor.UnitPrice.ShouldBe(product.UnitPrice);
        editor.VatRate.ShouldBe(25);
    }

    [Fact]
    public async Task SavingANewProductRaisesSaved()
    {
        var editor = Editor();
        Fill(editor);
        ProductDto? saved = null;
        editor.Saved += (_, dto) => saved = dto;

        await editor.SaveCommand.ExecuteAsync(null);

        saved.ShouldNotBeNull().Number.ShouldBe("HW-1");
        editor.ErrorMessage.ShouldBeNull();
    }

    [Fact]
    public async Task SavingAnExistingProductUpdatesIt()
    {
        var product = await AddAsync();
        var editor = Editor(product);
        editor.Name = "Cat7 cable";
        ProductDto? saved = null;
        editor.Saved += (_, dto) => saved = dto;

        await editor.SaveCommand.ExecuteAsync(null);

        saved.ShouldNotBeNull().Id.ShouldBe(product.Id);
        saved.Name.ShouldBe("Cat7 cable");
    }

    [Fact]
    public async Task AnEmptyPriceOrVatBoxIsReportedBeforeAnythingIsSent()
    {
        var editor = Editor();
        Fill(editor);
        var raised = false;
        editor.Saved += (_, _) => raised = true;

        editor.UnitPrice = null;
        await editor.SaveCommand.ExecuteAsync(null);
        editor.ErrorMessage.ShouldBe(Error("product_price_required"));

        editor.UnitPrice = 50;
        editor.VatRate = null;
        await editor.SaveCommand.ExecuteAsync(null);
        editor.ErrorMessage.ShouldBe(Error("product_vat_required"));
        raised.ShouldBeFalse();
    }

    [Fact]
    public async Task AMissingKindOrUnitIsReported()
    {
        var editor = Editor();
        Fill(editor);

        editor.SelectedUnit = null;
        await editor.SaveCommand.ExecuteAsync(null);
        editor.ErrorMessage.ShouldBe(Error("product_unit_invalid"));

        editor.SelectedKind = new ProductKindOption(null, "Pick one");
        await editor.SaveCommand.ExecuteAsync(null);
        editor.ErrorMessage.ShouldBe(Error("product_kind_invalid"));
    }

    [Fact]
    public async Task BlankNumberAndNameAreRejected()
    {
        var editor = Editor();
        editor.UnitPrice = 50;

        await editor.SaveCommand.ExecuteAsync(null);
        editor.ErrorMessage.ShouldBe(Error("product_number_required"));

        editor.Number = "HW-1";
        await editor.SaveCommand.ExecuteAsync(null);
        editor.ErrorMessage.ShouldBe(Error("product_name_required"));
    }

    [Theory]
    [InlineData(-1, 25, "product_price_invalid")]
    [InlineData(10, 101, "product_vat_invalid")]
    [InlineData(10, -1, "product_vat_invalid")]
    public async Task OutOfRangeMoneyIsRejected(int price, int vat, string code)
    {
        var editor = Editor();
        Fill(editor);
        editor.UnitPrice = price;
        editor.VatRate = vat;

        await editor.SaveCommand.ExecuteAsync(null);

        editor.ErrorMessage.ShouldBe(Error(code));
    }

    [Fact]
    public async Task ADuplicateNumberIsRejectedAndTheErrorClearsOnTheNextGoodSave()
    {
        await AddAsync();
        var editor = Editor();
        Fill(editor);

        await editor.SaveCommand.ExecuteAsync(null);
        editor.ErrorMessage.ShouldBe(Error("product_number_conflict"));

        editor.Number = "HW-2";
        await editor.SaveCommand.ExecuteAsync(null);
        editor.ErrorMessage.ShouldBeNull();
    }

    [Fact]
    public async Task AnUnknownCategoryIsRejected()
    {
        var editor = Editor();
        Fill(editor);
        editor.SelectedCategory = editor.Categories[1];

        await editor.SaveCommand.ExecuteAsync(null);

        editor.ErrorMessage.ShouldBe(Error("product_category_not_found"));
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

    // ---- detail ----

    private ProductDetailViewModel Detail(ProductDto product) => new(product, _localizer);

    private static string Money(decimal amount) => amount.ToString("N2", CultureInfo.CurrentCulture);

    [Fact]
    public async Task TheDetailFormatsPricesMarginAndVat()
    {
        var product = await AddAsync();
        var detail = Detail(product);
        var unit = _localizer.Get("ProductUnit_Piece");

        detail.KindLabel.ShouldBe(_localizer.Get("ProductKind_Hardware"));
        detail.StatusText.ShouldBe(_localizer.Get("ProductDetail_Active"));
        detail.CategoryText.ShouldBe(_localizer.Get("Products_NoCategory"));
        detail.PriceText.ShouldBe($"{Money(product.UnitPrice)} / {unit}");
        detail.PriceWithVatText.ShouldBe($"{Money(product.UnitPrice * 1.25m)} / {unit}");
        detail.CostText.ShouldBe($"{Money(20)} / {unit}");
        detail.VatText.ShouldBe(string.Create(CultureInfo.CurrentCulture, $"{25:0.##} %"));
        detail.MarginText.ShouldStartWith(Money(product.UnitPrice - 20));
        detail.IsHardware.ShouldBeTrue();
        detail.IsSoftware.ShouldBeFalse();
        detail.IsConsulting.ShouldBeFalse();
    }

    [Fact]
    public async Task AFreeProductHasNoMarginToShow()
    {
        var product = (await AddAsync()) with { UnitPrice = 0m };

        Detail(product).MarginText.ShouldBe("-");
    }

    [Fact]
    public async Task AnInactiveProductWithACategoryIsDescribedAsSuch()
    {
        var product = (await AddAsync()) with { IsActive = false, CategoryName = "Cables" };

        var detail = Detail(product);

        detail.StatusText.ShouldBe(_localizer.Get("ProductDetail_Inactive"));
        detail.CategoryText.ShouldBe("Cables");
    }

    [Fact]
    public async Task TheDetailHasNoOptionalPartsWithoutFactoriesAndBackAndEditRaiseEvents()
    {
        var detail = Detail(await AddAsync());
        var closed = 0;
        var edit = 0;
        detail.Closed += (_, _) => closed++;
        detail.EditRequested += (_, _) => edit++;

        detail.BackCommand.Execute(null);
        detail.EditCommand.Execute(null);

        detail.Photo.ShouldBeNull();
        detail.SupplierPriceList.ShouldBeNull();
        detail.StockCard.ShouldBeNull();
        closed.ShouldBe(1);
        edit.ShouldBe(1);
    }
}

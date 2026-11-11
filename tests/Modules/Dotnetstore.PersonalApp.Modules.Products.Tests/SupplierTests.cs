using Dotnetstore.PersonalApp.Modules.Products.Features.Products;
using Dotnetstore.PersonalApp.Modules.Products.Features.SupplierPrices;
using Dotnetstore.PersonalApp.Modules.Products.Features.Suppliers;

namespace Dotnetstore.PersonalApp.Modules.Products.Tests;

public sealed class SupplierTests : IDisposable
{
    private readonly ProductsFixture _fixture = new();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public void Dispose() => _fixture.Dispose();

    private static SupplierFields Fields(string name = "Kabelgrossisten", string? email = "order@kabel.se", bool active = true) =>
        new(name, " 556000-0000 ", email, null, null, null, active);

    private async Task<SupplierDto> CreateSupplierAsync(string name = "Kabelgrossisten", bool active = true) =>
        (await _fixture.CreateSupplier.HandleAsync(new CreateSupplier.Command(Fields(name, active: active)), Ct)).Value;

    private async Task<ProductDto> CreateProductAsync(string number = "HW-1") =>
        (await _fixture.Create.HandleAsync(new CreateProduct.Command(ProductsFixture.Hardware(number)), Ct)).Value;

    [Fact]
    public async Task CreatesASupplierAndTrimsOptionalText()
    {
        var supplier = await CreateSupplierAsync();

        supplier.Name.ShouldBe("Kabelgrossisten");
        supplier.OrganizationNumber.ShouldBe("556000-0000");
        supplier.Phone.ShouldBeNull();
    }

    [Fact]
    public async Task RejectsADuplicateNameIgnoringCase()
    {
        await CreateSupplierAsync("Kabelgrossisten");

        var result = await _fixture.CreateSupplier.HandleAsync(new CreateSupplier.Command(Fields("kabelgrossisten")), Ct);

        result.Error.Code.ShouldBe("supplier_name_conflict");
    }

    [Theory]
    [InlineData("", "order@kabel.se", "supplier_name_required")]
    [InlineData("Acme", "not-an-email", "supplier_email_invalid")]
    public async Task RejectsInvalidInput(string name, string email, string code)
    {
        var result = await _fixture.CreateSupplier.HandleAsync(new CreateSupplier.Command(Fields(name, email)), Ct);

        result.Error.Code.ShouldBe(code);
    }

    [Fact]
    public async Task UpdatesASupplier()
    {
        var supplier = await CreateSupplierAsync();

        var result = await _fixture.UpdateSupplier.HandleAsync(new UpdateSupplier.Command(supplier.Id, Fields("Renamed", active: false)), Ct);

        result.Value.Name.ShouldBe("Renamed");
        result.Value.IsActive.ShouldBeFalse();
    }

    [Fact]
    public async Task ListsOnlyActiveSuppliersForPickers()
    {
        await CreateSupplierAsync("Active");
        await CreateSupplierAsync("Dormant", active: false);

        var all = await _fixture.ListSuppliers.HandleAsync(new ListSuppliers.Query(), Ct);
        var active = await _fixture.ListSuppliers.HandleAsync(new ListSuppliers.Query(ActiveOnly: true), Ct);

        all.TotalCount.ShouldBe(2);
        active.Items.Select(s => s.Name).ShouldBe(["Active"]);
    }

    [Fact]
    public async Task SuppliersHaveDifferentPricesForTheSameProductCheapestFirst()
    {
        var product = await CreateProductAsync();
        var a = await CreateSupplierAsync("A-Supplier");
        var b = await CreateSupplierAsync("B-Supplier");

        await _fixture.SetPrice.HandleAsync(new SetSupplierPrice.Command(product.Id, a.Id, "A-1", 120m), Ct);
        await _fixture.SetPrice.HandleAsync(new SetSupplierPrice.Command(product.Id, b.Id, null, 99.5m), Ct);

        var prices = await _fixture.ListPrices.HandleAsync(new ListSupplierPrices.Query(product.Id), Ct);

        prices.Select(p => (p.SupplierName, p.Price)).ShouldBe([("B-Supplier", 99.5m), ("A-Supplier", 120m)]);
    }

    [Fact]
    public async Task SettingAPriceAgainReplacesIt()
    {
        var product = await CreateProductAsync();
        var supplier = await CreateSupplierAsync();

        await _fixture.SetPrice.HandleAsync(new SetSupplierPrice.Command(product.Id, supplier.Id, null, 100m), Ct);
        await _fixture.SetPrice.HandleAsync(new SetSupplierPrice.Command(product.Id, supplier.Id, " X-9 ", 80.126m), Ct);

        var price = (await _fixture.ListPrices.HandleAsync(new ListSupplierPrices.Query(product.Id), Ct)).ShouldHaveSingleItem();
        price.Price.ShouldBe(80.13m);
        price.SupplierArticleNumber.ShouldBe("X-9");
    }

    [Fact]
    public async Task RejectsBadPricesAndUnknownReferences()
    {
        var product = await CreateProductAsync();
        var supplier = await CreateSupplierAsync();

        var negative = await _fixture.SetPrice.HandleAsync(new SetSupplierPrice.Command(product.Id, supplier.Id, null, -1m), Ct);
        var noProduct = await _fixture.SetPrice.HandleAsync(new SetSupplierPrice.Command(Guid.NewGuid(), supplier.Id, null, 1m), Ct);
        var noSupplier = await _fixture.SetPrice.HandleAsync(new SetSupplierPrice.Command(product.Id, Guid.NewGuid(), null, 1m), Ct);

        negative.Error.Code.ShouldBe("supplier_price_invalid");
        noProduct.Error.Code.ShouldBe("product_not_found");
        noSupplier.Error.Code.ShouldBe("supplier_not_found");
    }

    [Fact]
    public async Task DeletesAPrice()
    {
        var product = await CreateProductAsync();
        var supplier = await CreateSupplierAsync();
        var price = (await _fixture.SetPrice.HandleAsync(new SetSupplierPrice.Command(product.Id, supplier.Id, null, 10m), Ct)).Value;

        var deleted = await _fixture.DeletePrice.HandleAsync(new DeleteSupplierPrice.Command(price.Id), Ct);
        var again = await _fixture.DeletePrice.HandleAsync(new DeleteSupplierPrice.Command(price.Id), Ct);

        deleted.IsSuccess.ShouldBeTrue();
        again.Error.Code.ShouldBe("supplier_price_not_found");
    }

    [Fact]
    public async Task DeletingASupplierOrAProductRemovesItsPrices()
    {
        var kept = await CreateProductAsync("HW-1");
        var other = await CreateProductAsync("HW-2");
        var supplier = await CreateSupplierAsync("Gone");
        var stays = await CreateSupplierAsync("Stays");
        await _fixture.SetPrice.HandleAsync(new SetSupplierPrice.Command(kept.Id, supplier.Id, null, 10m), Ct);
        await _fixture.SetPrice.HandleAsync(new SetSupplierPrice.Command(kept.Id, stays.Id, null, 11m), Ct);
        await _fixture.SetPrice.HandleAsync(new SetSupplierPrice.Command(other.Id, stays.Id, null, 12m), Ct);

        await _fixture.DeleteSupplier.HandleAsync(new DeleteSupplier.Command(supplier.Id), Ct);
        (await _fixture.ListPrices.HandleAsync(new ListSupplierPrices.Query(kept.Id), Ct)).Select(p => p.SupplierName).ShouldBe(["Stays"]);

        await _fixture.Delete.HandleAsync(new DeleteProduct.Command(other.Id), Ct);
        (await _fixture.ListPrices.HandleAsync(new ListSupplierPrices.Query(other.Id), Ct)).ShouldBeEmpty();
    }
}

using Dotnetstore.PersonalApp.Modules.Products.Domain;
using Dotnetstore.PersonalApp.Modules.Products.Features.ProductCategories;
using Dotnetstore.PersonalApp.Modules.Products.Features.Products;

namespace Dotnetstore.PersonalApp.Modules.Products.Tests;

public sealed class ProductHandlerTests : IDisposable
{
    private readonly ProductsFixture _fixture = new();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public void Dispose() => _fixture.Dispose();

    [Fact]
    public async Task CreatesAHardwareProductAndRoundsPrices()
    {
        var result = await _fixture.Create.HandleAsync(new CreateProduct.Command(ProductsFixture.Hardware(" HW-1 ")), Ct);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Number.ShouldBe("HW-1");
        result.Value.UnitPrice.ShouldBe(50.00m);
        result.Value.Manufacturer.ShouldBe("Acme");
    }

    [Fact]
    public async Task KeepsOnlyTheDetailsThatBelongToTheKind()
    {
        var fields = ProductsFixture.Hardware("SW-1") with
        {
            Kind = ProductKind.Software,
            Unit = ProductUnit.License,
            SoftwareVersion = "2.1",
            MinimumHours = 4m,
        };

        var result = await _fixture.Create.HandleAsync(new CreateProduct.Command(fields), Ct);

        result.IsSuccess.ShouldBeTrue();
        result.Value.SoftwareVersion.ShouldBe("2.1");
        result.Value.Manufacturer.ShouldBeNull();
        result.Value.Model.ShouldBeNull();
        result.Value.MinimumHours.ShouldBeNull();
    }

    [Fact]
    public async Task CreatesAConsultingProductWithMinimumHours()
    {
        var fields = ProductsFixture.Hardware("CON-1") with
        {
            Kind = ProductKind.Consulting,
            Unit = ProductUnit.Hour,
            MinimumHours = 2m,
        };

        var result = await _fixture.Create.HandleAsync(new CreateProduct.Command(fields), Ct);

        result.Value.MinimumHours.ShouldBe(2m);
        result.Value.Manufacturer.ShouldBeNull();
    }

    [Fact]
    public async Task RejectsADuplicateNumberIgnoringCase()
    {
        await _fixture.Create.HandleAsync(new CreateProduct.Command(ProductsFixture.Hardware("HW-1")), Ct);

        var result = await _fixture.Create.HandleAsync(new CreateProduct.Command(ProductsFixture.Hardware("hw-1")), Ct);

        result.Error.Code.ShouldBe("product_number_conflict");
    }

    [Fact]
    public async Task RejectsAnEmptyNumber()
    {
        var result = await _fixture.Create.HandleAsync(new CreateProduct.Command(ProductsFixture.Hardware(" ")), Ct);

        result.Error.Code.ShouldBe("product_number_required");
    }

    [Fact]
    public async Task RejectsAnUnknownCategory()
    {
        var result = await _fixture.Create.HandleAsync(new CreateProduct.Command(ProductsFixture.Hardware(categoryId: Guid.NewGuid())), Ct);

        result.Error.Code.ShouldBe("product_category_not_found");
    }

    [Fact]
    public async Task UpdatesAProduct()
    {
        var created = await _fixture.Create.HandleAsync(new CreateProduct.Command(ProductsFixture.Hardware()), Ct);

        var result = await _fixture.Update.HandleAsync(
            new UpdateProduct.Command(created.Value.Id, ProductsFixture.Hardware() with { Name = "Renamed", IsActive = false }), Ct);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Name.ShouldBe("Renamed");
        result.Value.IsActive.ShouldBeFalse();
    }

    [Fact]
    public async Task UpdateMayKeepItsOwnNumberButNotTakeAnothers()
    {
        var first = await _fixture.Create.HandleAsync(new CreateProduct.Command(ProductsFixture.Hardware("A")), Ct);
        await _fixture.Create.HandleAsync(new CreateProduct.Command(ProductsFixture.Hardware("B")), Ct);

        var same = await _fixture.Update.HandleAsync(new UpdateProduct.Command(first.Value.Id, ProductsFixture.Hardware("A")), Ct);
        var taken = await _fixture.Update.HandleAsync(new UpdateProduct.Command(first.Value.Id, ProductsFixture.Hardware("b")), Ct);

        same.IsSuccess.ShouldBeTrue();
        taken.Error.Code.ShouldBe("product_number_conflict");
    }

    [Fact]
    public async Task UpdateOfAMissingProductIsNotFound()
    {
        var result = await _fixture.Update.HandleAsync(new UpdateProduct.Command(Guid.NewGuid(), ProductsFixture.Hardware()), Ct);

        result.Error.Code.ShouldBe("product_not_found");
    }

    [Fact]
    public async Task DeletesAProduct()
    {
        var created = await _fixture.Create.HandleAsync(new CreateProduct.Command(ProductsFixture.Hardware()), Ct);

        var result = await _fixture.Delete.HandleAsync(new DeleteProduct.Command(created.Value.Id), Ct);
        var again = await _fixture.Delete.HandleAsync(new DeleteProduct.Command(created.Value.Id), Ct);

        result.IsSuccess.ShouldBeTrue();
        again.Error.Code.ShouldBe("product_not_found");
    }

    [Fact]
    public async Task ListsFilteredByKindAndSearch()
    {
        await _fixture.Create.HandleAsync(new CreateProduct.Command(ProductsFixture.Hardware("HW-1")), Ct);
        await _fixture.Create.HandleAsync(
            new CreateProduct.Command(ProductsFixture.Hardware("CON-1") with { Name = "Consulting", Kind = ProductKind.Consulting, Unit = ProductUnit.Hour }), Ct);

        var all = await _fixture.List.HandleAsync(new ListProducts.Query(), Ct);
        var consulting = await _fixture.List.HandleAsync(new ListProducts.Query(Kind: ProductKind.Consulting), Ct);
        var searched = await _fixture.List.HandleAsync(new ListProducts.Query(Search: "acme"), Ct);

        all.TotalCount.ShouldBe(2);
        consulting.Items.Select(p => p.Number).ShouldBe(["CON-1"]);
        searched.Items.Select(p => p.Number).ShouldBe(["HW-1"]);
    }

    [Fact]
    public async Task PagesTheList()
    {
        for (var i = 1; i <= 5; i++)
        {
            await _fixture.Create.HandleAsync(new CreateProduct.Command(ProductsFixture.Hardware($"P-{i}") with { Name = $"Item {i}" }), Ct);
        }

        var page = await _fixture.List.HandleAsync(new ListProducts.Query(Page: 2, PageSize: 2), Ct);

        page.TotalCount.ShouldBe(5);
        page.Items.Count.ShouldBe(2);
    }

    [Fact]
    public async Task ACategoryInUseCannotBeDeleted()
    {
        var category = await _fixture.CreateCategory.HandleAsync(new CreateProductCategory.Command("Cables", null), Ct);
        var product = await _fixture.Create.HandleAsync(new CreateProduct.Command(ProductsFixture.Hardware(categoryId: category.Value.Id)), Ct);

        var blocked = await _fixture.DeleteCategory.HandleAsync(new DeleteProductCategory.Command(category.Value.Id), Ct);
        await _fixture.Delete.HandleAsync(new DeleteProduct.Command(product.Value.Id), Ct);
        var allowed = await _fixture.DeleteCategory.HandleAsync(new DeleteProductCategory.Command(category.Value.Id), Ct);

        blocked.Error.Code.ShouldBe("product_category_in_use");
        allowed.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task CategoryNamesAreUniqueIgnoringCase()
    {
        await _fixture.CreateCategory.HandleAsync(new CreateProductCategory.Command("Cables", null), Ct);

        var result = await _fixture.CreateCategory.HandleAsync(new CreateProductCategory.Command("cables", null), Ct);

        result.Error.Code.ShouldBe("product_category_conflict");
    }
}

using Dotnetstore.PersonalApp.Modules.Products.Domain;
using Dotnetstore.PersonalApp.Modules.Products.Features.ProductCategories;
using Dotnetstore.PersonalApp.Modules.Products.Features.Products;
using Dotnetstore.PersonalApp.Modules.Products.Features.StorageLocations;

namespace Dotnetstore.PersonalApp.Modules.Products.Tests;

public sealed class CategoryLocationAndCatalogTests : IDisposable
{
    private readonly ProductsFixture _fixture = new();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public void Dispose() => _fixture.Dispose();

    private async Task<ProductCategoryDto> CategoryAsync(string name, string? description = null) =>
        (await _fixture.CreateCategory.HandleAsync(new CreateProductCategory.Command(name, description), Ct)).Value;

    [Fact]
    public async Task GetsACategoryById()
    {
        var created = await CategoryAsync("Cables", "All cables");

        var result = await new GetProductCategory.Handler(_fixture.DbFactory).HandleAsync(new GetProductCategory.Query(created.Id), Ct);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Name.ShouldBe("Cables");
        result.Value.Description.ShouldBe("All cables");
    }

    [Fact]
    public async Task AnUnknownCategoryIsNotFound()
    {
        var result = await new GetProductCategory.Handler(_fixture.DbFactory).HandleAsync(new GetProductCategory.Query(Guid.NewGuid()), Ct);

        result.IsSuccess.ShouldBeFalse();
        result.Error.Code.ShouldBe("product_category_not_found");
    }

    [Fact]
    public async Task ListsCategoriesByNameAndSearchesNameAndDescription()
    {
        await CategoryAsync("Monitors");
        await CategoryAsync("Cables", "Network and power");
        var list = new ListProductCategories.Handler(_fixture.DbFactory);

        var all = await list.HandleAsync(new ListProductCategories.Query(), Ct);
        var searched = await list.HandleAsync(new ListProductCategories.Query("power"), Ct);

        all.TotalCount.ShouldBe(2);
        all.Items.Select(i => i.Name).ShouldBe(["Cables", "Monitors"]);
        searched.Items.Select(i => i.Name).ShouldBe(["Cables"]);
    }

    [Fact]
    public async Task UpdatesACategoryAndTrimsTheName()
    {
        var created = await CategoryAsync("Cables");
        var update = new UpdateProductCategory.Handler(_fixture.DbFactory, new UpdateProductCategory.Validator());

        var result = await update.HandleAsync(new UpdateProductCategory.Command(created.Id, "  Wires ", "Thin"), Ct);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Name.ShouldBe("Wires");
        result.Value.Description.ShouldBe("Thin");
    }

    [Fact]
    public async Task ACategoryCannotTakeTheNameOfAnotherButMayKeepItsOwn()
    {
        var cables = await CategoryAsync("Cables");
        await CategoryAsync("Monitors");
        var update = new UpdateProductCategory.Handler(_fixture.DbFactory, new UpdateProductCategory.Validator());

        var clash = await update.HandleAsync(new UpdateProductCategory.Command(cables.Id, "Monitors", null), Ct);
        var same = await update.HandleAsync(new UpdateProductCategory.Command(cables.Id, "Cables", "Now with text"), Ct);

        clash.Error.Code.ShouldBe("product_category_conflict");
        same.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task UpdatingAnUnknownCategoryIsNotFoundAndABlankNameIsRejected()
    {
        var update = new UpdateProductCategory.Handler(_fixture.DbFactory, new UpdateProductCategory.Validator());
        var created = await CategoryAsync("Cables");

        (await update.HandleAsync(new UpdateProductCategory.Command(Guid.NewGuid(), "X", null), Ct)).Error.Code.ShouldBe("product_category_not_found");
        (await update.HandleAsync(new UpdateProductCategory.Command(created.Id, " ", null), Ct)).IsSuccess.ShouldBeFalse();
    }

    [Fact]
    public async Task ListsStorageLocationsByNameWithSearchAndActiveFilter()
    {
        await _fixture.CreateLocation.HandleAsync(new CreateStorageLocation.Command(new StorageLocationFields("Warehouse", "Main building", true)), Ct);
        await _fixture.CreateLocation.HandleAsync(new CreateStorageLocation.Command(new StorageLocationFields("Attic", null, false)), Ct);
        var list = new ListStorageLocations.Handler(_fixture.DbFactory);

        var all = await list.HandleAsync(new ListStorageLocations.Query(), Ct);
        var active = await list.HandleAsync(new ListStorageLocations.Query(ActiveOnly: true), Ct);
        var searched = await list.HandleAsync(new ListStorageLocations.Query("building"), Ct);

        all.Items.Select(i => i.Name).ShouldBe(["Attic", "Warehouse"]);
        active.Items.Select(i => i.Name).ShouldBe(["Warehouse"]);
        searched.Items.Select(i => i.Name).ShouldBe(["Warehouse"]);
    }

    [Fact]
    public async Task TheCatalogOffersActiveProductsByNameAndMarksConsultingAsNotStockable()
    {
        await _fixture.Create.HandleAsync(new CreateProduct.Command(ProductsFixture.Hardware("HW-1") with { Name = "Cable" }), Ct);
        await _fixture.Create.HandleAsync(new CreateProduct.Command(ProductsFixture.Hardware("HW-2") with { Name = "Retired", IsActive = false }), Ct);
        await _fixture.Create.HandleAsync(
            new CreateProduct.Command(ProductsFixture.Hardware("CO-1") with { Name = "Advice", Kind = ProductKind.Consulting, Unit = ProductUnit.Hour, MinimumHours = 1m }),
            Ct);
        var catalog = new ProductCatalog(_fixture.DbFactory);

        var all = await catalog.SearchAsync(null, 10, Ct);
        var searched = await catalog.SearchAsync("hw-", 10, Ct);

        all.Select(p => p.Name).ShouldBe(["Advice", "Cable"]);
        all.Single(p => p.Number == "CO-1").IsStockable.ShouldBeFalse();
        all.Single(p => p.Number == "HW-1").IsStockable.ShouldBeTrue();
        searched.Select(p => p.Number).ShouldBe(["HW-1"]);
    }

    [Fact]
    public async Task TheCatalogLimitsHowManyProductsItReturns()
    {
        for (var i = 1; i <= 3; i++)
        {
            await _fixture.Create.HandleAsync(new CreateProduct.Command(ProductsFixture.Hardware($"HW-{i}")), Ct);
        }

        (await new ProductCatalog(_fixture.DbFactory).SearchAsync(null, 2, Ct)).Count.ShouldBe(2);
    }
}

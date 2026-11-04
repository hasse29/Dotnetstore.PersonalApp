using Dotnetstore.PersonalApp.Modules.Production.Features;
using Dotnetstore.PersonalApp.Modules.Production.Features.ProductCostHistories;
using Dotnetstore.PersonalApp.Modules.Production.Features.Products;
using Microsoft.Extensions.DependencyInjection;

namespace Dotnetstore.PersonalApp.Modules.Production.Tests;

public sealed class ProductDetailTests : IDisposable
{
    private readonly ProductionFixture _fixture = new();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public void Dispose() => _fixture.Dispose();

    [Fact]
    public async Task HistoryCanBeListedForOneProductOnly()
    {
        var first = await _fixture.MakeProductCostHistoryAsync(Ct);
        await _fixture.MakeProductCostHistoryAsync(Ct);

        var history = await _fixture.Services.GetRequiredService<ListProductCostHistories.Handler>()
            .HandleAsync(new ListProductCostHistories.Query(ParentId: first.ProductId), Ct);

        history.Items.ShouldHaveSingleItem().Id.ShouldBe(first.Id);
    }

    [Fact]
    public async Task ANewRowOnAnEmbeddedPageBelongsToTheProductAndRaisesChangedWhenSaved()
    {
        var product = await _fixture.MakeProductAsync(Ct);
        var page = _fixture.Services.GetRequiredService<ProductCostHistoriesViewModel>().ForParent(product.Id);
        var changed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        page.Changed += (_, _) => changed.TrySetResult();

        await page.LoadCommand.ExecuteAsync(null);
        page.NewCommand.Execute(null);
        var editor = page.Editor.ShouldNotBeNull();
        editor.IsParentFixed.ShouldBeTrue();
        editor.SelectedProduct.ShouldNotBeNull().Id.ShouldBe(product.Id);
        editor.StandardCost = 5m;
        await editor.SaveCommand.ExecuteAsync(null);

        // The page reloads before it reports, in an event handler, so wait for the report.
        await changed.Task.WaitAsync(TimeSpan.FromSeconds(5), Ct);
        page.ProductCostHistories.ShouldHaveSingleItem().ProductId.ShouldBe(product.Id);
    }

    [Fact]
    public async Task TheProductDetailIsBuiltFromTheContainerAndHasAView()
    {
        var product = await _fixture.MakeProductAsync(Ct);

        var detail = _fixture.Services.GetRequiredService<IDetailFactory<ProductDto>>().Create(product);

        var productDetail = detail.ShouldBeOfType<ProductDetailViewModel>();
        productDetail.Product.Id.ShouldBe(product.Id);
        productDetail.CostHistory.IsEmbedded.ShouldBeTrue();
        productDetail.BillOfMaterials.IsEmbedded.ShouldBeTrue();
        typeof(ProductDetailViewModel).Assembly.GetType("Dotnetstore.PersonalApp.Modules.Production.Features.Products.ProductDetailView").ShouldNotBeNull();
    }
}

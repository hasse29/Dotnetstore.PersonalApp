using Dotnetstore.PersonalApp.Modules.Products.Domain;
using Dotnetstore.PersonalApp.Modules.Products.Features.Products;
using Dotnetstore.PersonalApp.Modules.Products.Features.Stock;
using Dotnetstore.PersonalApp.Modules.Products.Features.StorageLocations;
using Dotnetstore.PersonalApp.Shared.Stock;

namespace Dotnetstore.PersonalApp.Modules.Products.Tests;

public sealed class StockServiceTests : IDisposable
{
    private readonly ProductsFixture _fixture = new();
    private readonly StockService _service;

    public StockServiceTests() => _service = new StockService(_fixture.DbFactory, _fixture.Clock);

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public void Dispose() => _fixture.Dispose();

    private async Task<StorageLocationDto> LocationAsync(string name) =>
        (await _fixture.CreateLocation.HandleAsync(new CreateStorageLocation.Command(new StorageLocationFields(name, null, true)), Ct)).Value;

    private async Task<ProductDto> ProductAsync(string number = "HW-1") =>
        (await _fixture.Create.HandleAsync(new CreateProduct.Command(ProductsFixture.Hardware(number)), Ct)).Value;

    private async Task<decimal> TotalAsync(Guid productId) => (await _fixture.ProductStock.HandleAsync(new GetProductStock.Query(productId), Ct)).Total;

    [Fact]
    public async Task IssuesFromTheLocationsWithTheMostStockFirst()
    {
        var small = await LocationAsync("Small");
        var large = await LocationAsync("Large");
        var product = await ProductAsync();
        await _fixture.Receive.HandleAsync(new ReceiveStock.Command(product.Id, small.Id, 3m, null, null), Ct);
        await _fixture.Receive.HandleAsync(new ReceiveStock.Command(product.Id, large.Id, 8m, null, null), Ct);

        var result = await _service.IssueAsync(product.Id, 10m, "order", Ct);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe([new StockAllocation(large.Id, 8m), new StockAllocation(small.Id, 2m)]);
        (await TotalAsync(product.Id)).ShouldBe(1m);
    }

    [Fact]
    public async Task FailsWithoutChangingAnythingWhenThereIsNotEnough()
    {
        var location = await LocationAsync("Shelf");
        var product = await ProductAsync();
        await _fixture.Receive.HandleAsync(new ReceiveStock.Command(product.Id, location.Id, 3m, null, null), Ct);

        var result = await _service.IssueAsync(product.Id, 4m, "order", Ct);

        result.Error.Code.ShouldBe("stock_insufficient_total");
        (await TotalAsync(product.Id)).ShouldBe(3m);
    }

    [Fact]
    public async Task ReturnsGoodsToTheLocationsTheyCameFrom()
    {
        var a = await LocationAsync("A");
        var b = await LocationAsync("B");
        var product = await ProductAsync();
        await _fixture.Receive.HandleAsync(new ReceiveStock.Command(product.Id, a.Id, 5m, null, null), Ct);
        await _fixture.Receive.HandleAsync(new ReceiveStock.Command(product.Id, b.Id, 4m, null, null), Ct);
        var issued = (await _service.IssueAsync(product.Id, 7m, "order", Ct)).Value;

        var returned = await _service.ReturnAsync(product.Id, issued, "order cancelled", Ct);

        returned.IsSuccess.ShouldBeTrue();
        (await TotalAsync(product.Id)).ShouldBe(9m);
        var stock = await _fixture.ProductStock.HandleAsync(new GetProductStock.Query(product.Id), Ct);
        stock.Locations.Single(l => l.LocationId == a.Id).Quantity.ShouldBe(5m);
        stock.Locations.Single(l => l.LocationId == b.Id).Quantity.ShouldBe(4m);
    }

    [Fact]
    public async Task ConsultingCannotBeIssued()
    {
        var consulting = (await _fixture.Create.HandleAsync(
            new CreateProduct.Command(ProductsFixture.Hardware("CON-1") with { Kind = ProductKind.Consulting }), Ct)).Value;

        var result = await _service.IssueAsync(consulting.Id, 1m, "order", Ct);

        result.IsSuccess.ShouldBeFalse();
    }
}

using Dotnetstore.PersonalApp.Modules.Products.Domain;
using Dotnetstore.PersonalApp.Modules.Products.Features.InventoryCounts;
using Dotnetstore.PersonalApp.Modules.Products.Features.Products;
using Dotnetstore.PersonalApp.Modules.Products.Features.Stock;
using Dotnetstore.PersonalApp.Modules.Products.Features.Suppliers;
using Dotnetstore.PersonalApp.Modules.Products.Features.StorageLocations;

namespace Dotnetstore.PersonalApp.Modules.Products.Tests;

public sealed class StockTests : IDisposable
{
    private readonly ProductsFixture _fixture = new();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public void Dispose() => _fixture.Dispose();

    private async Task<StorageLocationDto> LocationAsync(string name = "Shelf A", bool active = true) =>
        (await _fixture.CreateLocation.HandleAsync(new CreateStorageLocation.Command(new StorageLocationFields(name, null, active)), Ct)).Value;

    private async Task<ProductDto> ProductAsync(string number = "HW-1", decimal? minimumStock = null) =>
        (await _fixture.Create.HandleAsync(new CreateProduct.Command(ProductsFixture.Hardware(number) with { MinimumStock = minimumStock }), Ct)).Value;

    private async Task<decimal> TotalAsync(Guid productId) => (await _fixture.ProductStock.HandleAsync(new GetProductStock.Query(productId), Ct)).Total;

    // Locations

    [Fact]
    public async Task LocationNamesAreUniqueIgnoringCase()
    {
        await LocationAsync("Shelf A");

        var result = await _fixture.CreateLocation.HandleAsync(new CreateStorageLocation.Command(new StorageLocationFields("shelf a", null, true)), Ct);

        result.Error.Code.ShouldBe("storage_location_conflict");
    }

    [Fact]
    public async Task ALocationWithoutHistoryCanBeDeletedButOneWithStockCannot()
    {
        var empty = await LocationAsync("Empty");
        var used = await LocationAsync("Used");
        var product = await ProductAsync();
        await _fixture.Receive.HandleAsync(new ReceiveStock.Command(product.Id, used.Id, 5m, null, null), Ct);

        (await _fixture.DeleteLocation.HandleAsync(new DeleteStorageLocation.Command(empty.Id), Ct)).IsSuccess.ShouldBeTrue();
        (await _fixture.DeleteLocation.HandleAsync(new DeleteStorageLocation.Command(used.Id), Ct)).Error.Code.ShouldBe("storage_location_in_use");
    }

    // Receiving, issuing, transferring

    [Fact]
    public async Task ReceivingAddsToTheLocationAndRecordsTheSupplier()
    {
        var location = await LocationAsync();
        var product = await ProductAsync();
        var supplier = (await _fixture.CreateSupplier.HandleAsync(new CreateSupplier.Command(new SupplierFields("Kabel AB", null, null, null, null, null, true)), Ct)).Value;

        var first = await _fixture.Receive.HandleAsync(new ReceiveStock.Command(product.Id, location.Id, 10m, supplier.Id, " delivery "), Ct);
        var second = await _fixture.Receive.HandleAsync(new ReceiveStock.Command(product.Id, location.Id, 2.5m, null, null), Ct);

        first.Value.ShouldBe(10m);
        second.Value.ShouldBe(12.5m);
        var movement = (await _fixture.Movements.HandleAsync(new ListStockMovements.Query(Type: StockMovementType.Receipt), Ct)).Items.Single(m => m.SupplierName is not null);
        movement.SupplierName.ShouldBe("Kabel AB");
        movement.Note.ShouldBe("delivery");
    }

    [Fact]
    public async Task IssuingTakesStockOutAndCannotGoBelowZero()
    {
        var location = await LocationAsync();
        var product = await ProductAsync();
        await _fixture.Receive.HandleAsync(new ReceiveStock.Command(product.Id, location.Id, 5m, null, null), Ct);

        var issued = await _fixture.Issue.HandleAsync(new IssueStock.Command(product.Id, location.Id, 3m, null), Ct);
        var tooMany = await _fixture.Issue.HandleAsync(new IssueStock.Command(product.Id, location.Id, 3m, null), Ct);

        issued.Value.ShouldBe(2m);
        tooMany.Error.Code.ShouldBe("stock_insufficient");
        (await TotalAsync(product.Id)).ShouldBe(2m);
        (await _fixture.Movements.HandleAsync(new ListStockMovements.Query(), Ct)).TotalCount.ShouldBe(2);
    }

    [Fact]
    public async Task ATransferMovesStockBetweenLocationsAsTwoMovements()
    {
        var a = await LocationAsync("A");
        var b = await LocationAsync("B");
        var product = await ProductAsync();
        await _fixture.Receive.HandleAsync(new ReceiveStock.Command(product.Id, a.Id, 10m, null, null), Ct);

        var result = await _fixture.Transfer.HandleAsync(new TransferStock.Command(product.Id, a.Id, b.Id, 4m, null), Ct);

        result.Value.ShouldBe(6m);
        var stock = await _fixture.ProductStock.HandleAsync(new GetProductStock.Query(product.Id), Ct);
        stock.Total.ShouldBe(10m);
        stock.Locations.Select(l => (l.LocationName, l.Quantity)).ShouldBe([("A", 6m), ("B", 4m)]);
        (await _fixture.Movements.HandleAsync(new ListStockMovements.Query(), Ct)).TotalCount.ShouldBe(3);
    }

    [Fact]
    public async Task AFailedTransferChangesNothing()
    {
        var a = await LocationAsync("A");
        var b = await LocationAsync("B");
        var product = await ProductAsync();
        await _fixture.Receive.HandleAsync(new ReceiveStock.Command(product.Id, a.Id, 2m, null, null), Ct);

        var tooMany = await _fixture.Transfer.HandleAsync(new TransferStock.Command(product.Id, a.Id, b.Id, 5m, null), Ct);
        var same = await _fixture.Transfer.HandleAsync(new TransferStock.Command(product.Id, a.Id, a.Id, 1m, null), Ct);

        tooMany.Error.Code.ShouldBe("stock_insufficient");
        same.Error.Code.ShouldBe("stock_same_location");
        (await TotalAsync(product.Id)).ShouldBe(2m);
        (await _fixture.Movements.HandleAsync(new ListStockMovements.Query(), Ct)).TotalCount.ShouldBe(1);
    }

    [Fact]
    public async Task RejectsBadQuantitiesConsultingAndInactiveLocations()
    {
        var location = await LocationAsync();
        var dormant = await LocationAsync("Dormant", active: false);
        var product = await ProductAsync();
        var consulting = (await _fixture.Create.HandleAsync(
            new CreateProduct.Command(ProductsFixture.Hardware("CON-1") with { Kind = ProductKind.Consulting, Unit = ProductUnit.Hour }), Ct)).Value;

        (await _fixture.Receive.HandleAsync(new ReceiveStock.Command(product.Id, location.Id, 0m, null, null), Ct)).Error.Code.ShouldBe("stock_quantity_invalid");
        (await _fixture.Receive.HandleAsync(new ReceiveStock.Command(consulting.Id, location.Id, 1m, null, null), Ct)).Error.Code.ShouldBe("product_not_stockable");
        (await _fixture.Receive.HandleAsync(new ReceiveStock.Command(product.Id, dormant.Id, 1m, null, null), Ct)).Error.Code.ShouldBe("storage_location_inactive");
        (await _fixture.Receive.HandleAsync(new ReceiveStock.Command(Guid.NewGuid(), location.Id, 1m, null, null), Ct)).Error.Code.ShouldBe("product_not_found");
        (await _fixture.Receive.HandleAsync(new ReceiveStock.Command(product.Id, Guid.NewGuid(), 1m, null, null), Ct)).Error.Code.ShouldBe("storage_location_not_found");
    }

    [Fact]
    public async Task StockCanStillBeIssuedFromAnInactiveLocation()
    {
        var location = await LocationAsync();
        var product = await ProductAsync();
        await _fixture.Receive.HandleAsync(new ReceiveStock.Command(product.Id, location.Id, 3m, null, null), Ct);
        await _fixture.UpdateLocation.HandleAsync(new UpdateStorageLocation.Command(location.Id, new StorageLocationFields("Shelf A", null, false)), Ct);

        var result = await _fixture.Issue.HandleAsync(new IssueStock.Command(product.Id, location.Id, 3m, null), Ct);

        result.IsSuccess.ShouldBeTrue();
    }

    // Overview and history

    [Fact]
    public async Task TheOverviewShowsTotalsLowStockAndLeavesOutConsulting()
    {
        var location = await LocationAsync();
        var low = await ProductAsync("LOW", minimumStock: 5m);
        var fine = await ProductAsync("FINE", minimumStock: 5m);
        await _fixture.Create.HandleAsync(
            new CreateProduct.Command(ProductsFixture.Hardware("CON-1") with { Kind = ProductKind.Consulting, Unit = ProductUnit.Hour }), Ct);
        await _fixture.Receive.HandleAsync(new ReceiveStock.Command(low.Id, location.Id, 5m, null, null), Ct);
        await _fixture.Receive.HandleAsync(new ReceiveStock.Command(fine.Id, location.Id, 9m, null, null), Ct);

        var all = await _fixture.Overview.HandleAsync(new ListStockOverview.Query(), Ct);
        var onlyLow = await _fixture.Overview.HandleAsync(new ListStockOverview.Query(LowOnly: true), Ct);

        all.Items.Select(i => i.Number).Order().ShouldBe(["FINE", "LOW"]);
        onlyLow.Items.Select(i => i.Number).ShouldBe(["LOW"]);
    }

    [Fact]
    public async Task ProductsThatHaveHadStockCannotBeDeleted()
    {
        var location = await LocationAsync();
        var product = await ProductAsync();
        await _fixture.Receive.HandleAsync(new ReceiveStock.Command(product.Id, location.Id, 1m, null, null), Ct);

        var result = await _fixture.Delete.HandleAsync(new DeleteProduct.Command(product.Id), Ct);

        result.Error.Code.ShouldBe("product_in_use");
    }

    // Inventory counts

    [Fact]
    public async Task ACountListsTheStockAndPostingAdjustsToWhatWasCounted()
    {
        var location = await LocationAsync();
        var up = await ProductAsync("UP");
        var down = await ProductAsync("DOWN");
        var skipped = await ProductAsync("SKIP");
        foreach (var (product, quantity) in new[] { (up, 5m), (down, 5m), (skipped, 5m) })
        {
            await _fixture.Receive.HandleAsync(new ReceiveStock.Command(product.Id, location.Id, quantity, null, null), Ct);
        }

        var count = (await _fixture.CreateCount.HandleAsync(new CreateInventoryCount.Command("Autumn", null, null, false), Ct)).Value;
        var detail = (await _fixture.GetCount.HandleAsync(new GetInventoryCount.Query(count.Id), Ct)).Value;
        detail.Lines.Count.ShouldBe(3);

        Guid LineOf(string number) => detail.Lines.Single(l => l.ProductNumber == number).Id;
        await _fixture.SaveCount.HandleAsync(
            new SaveInventoryCountLines.Command(count.Id, [new(LineOf("UP"), 8m), new(LineOf("DOWN"), 2m)]), Ct);

        var posted = await _fixture.PostCount.HandleAsync(new PostInventoryCount.Command(count.Id), Ct);

        posted.Value.ShouldBe(2);
        (await TotalAsync(up.Id)).ShouldBe(8m);
        (await TotalAsync(down.Id)).ShouldBe(2m);
        (await TotalAsync(skipped.Id)).ShouldBe(5m);
        (await _fixture.Movements.HandleAsync(new ListStockMovements.Query(Type: StockMovementType.CountAdjustment), Ct)).Items
            .Select(m => m.Quantity).Order().ShouldBe([-3m, 3m]);
    }

    [Fact]
    public async Task APostedCountIsFinal()
    {
        var location = await LocationAsync();
        var product = await ProductAsync();
        await _fixture.Receive.HandleAsync(new ReceiveStock.Command(product.Id, location.Id, 5m, null, null), Ct);
        var count = (await _fixture.CreateCount.HandleAsync(new CreateInventoryCount.Command("Once", null, null, false), Ct)).Value;
        await _fixture.PostCount.HandleAsync(new PostInventoryCount.Command(count.Id), Ct);

        var again = await _fixture.PostCount.HandleAsync(new PostInventoryCount.Command(count.Id), Ct);
        var save = await _fixture.SaveCount.HandleAsync(new SaveInventoryCountLines.Command(count.Id, []), Ct);

        again.Error.Code.ShouldBe("inventory_count_posted");
        save.Error.Code.ShouldBe("inventory_count_posted");
    }

    [Fact]
    public async Task PostingComparesWithTheStockAsItIsNowAndCanAddUnstockedProducts()
    {
        var location = await LocationAsync();
        var known = await ProductAsync("KNOWN");
        var found = await ProductAsync("FOUND");
        await _fixture.Receive.HandleAsync(new ReceiveStock.Command(known.Id, location.Id, 5m, null, null), Ct);
        var count = (await _fixture.CreateCount.HandleAsync(new CreateInventoryCount.Command("Shelf A", location.Id, null, IncludeUnstocked: true), Ct)).Value;
        var detail = (await _fixture.GetCount.HandleAsync(new GetInventoryCount.Query(count.Id), Ct)).Value;
        detail.Lines.Select(l => (l.ProductNumber, l.ExpectedQuantity)).Order().ShouldBe([("FOUND", 0m), ("KNOWN", 5m)]);

        // Two are sold after the count was started; the shelf still held 5 when it was counted.
        await _fixture.Issue.HandleAsync(new IssueStock.Command(known.Id, location.Id, 2m, null), Ct);
        await _fixture.SaveCount.HandleAsync(
            new SaveInventoryCountLines.Command(count.Id, detail.Lines.Select(l => new SaveInventoryCountLines.Entry(l.Id, l.ProductNumber == "FOUND" ? 4m : 5m)).ToList()), Ct);
        await _fixture.PostCount.HandleAsync(new PostInventoryCount.Command(count.Id), Ct);

        (await TotalAsync(known.Id)).ShouldBe(5m);
        (await TotalAsync(found.Id)).ShouldBe(4m);
    }

    [Fact]
    public async Task CountInputIsValidated()
    {
        var location = await LocationAsync();

        (await _fixture.CreateCount.HandleAsync(new CreateInventoryCount.Command(" ", null, null, false), Ct)).Error.Code.ShouldBe("inventory_count_name_required");
        (await _fixture.CreateCount.HandleAsync(new CreateInventoryCount.Command("X", null, null, true), Ct)).Error.Code.ShouldBe("inventory_count_location_required");
        (await _fixture.CreateCount.HandleAsync(new CreateInventoryCount.Command("X", Guid.NewGuid(), null, false), Ct)).Error.Code.ShouldBe("storage_location_not_found");

        var count = (await _fixture.CreateCount.HandleAsync(new CreateInventoryCount.Command("OK", location.Id, null, false), Ct)).Value;
        (await _fixture.SaveCount.HandleAsync(new SaveInventoryCountLines.Command(count.Id, [new(Guid.NewGuid(), -1m)]), Ct)).Error.Code.ShouldBe("stock_counted_invalid");
        (await _fixture.DeleteCount.HandleAsync(new DeleteInventoryCount.Command(count.Id), Ct)).IsSuccess.ShouldBeTrue();
        (await _fixture.ListCounts.HandleAsync(new ListInventoryCounts.Query(), Ct)).TotalCount.ShouldBe(0);
    }
}

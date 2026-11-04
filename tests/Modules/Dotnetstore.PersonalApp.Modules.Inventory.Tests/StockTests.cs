using Dotnetstore.PersonalApp.Modules.Inventory.Features.InventoryCountLines;
using Dotnetstore.PersonalApp.Modules.Inventory.Features.InventoryCounts;
using Dotnetstore.PersonalApp.Modules.Inventory.Features.Receiving;
using Dotnetstore.PersonalApp.Modules.Inventory.Features.StockMovements;
using Dotnetstore.PersonalApp.Modules.Inventory.Features.StockOverview;
using Dotnetstore.PersonalApp.Modules.Inventory.Features.StorageLocations;
using Dotnetstore.PersonalApp.Modules.Inventory.Features.Transfers;
using Dotnetstore.PersonalApp.Modules.Inventory.Features.Warehouses;
using Dotnetstore.PersonalApp.Shared.Contracts.Events;
using Microsoft.Extensions.DependencyInjection;

namespace Dotnetstore.PersonalApp.Modules.Inventory.Tests;

public sealed class StockTests : IDisposable
{
    private static readonly DateOnly Today = new(2026, 1, 1);

    private readonly InventoryFixture _fixture = new();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public void Dispose() => _fixture.Dispose();

    private T Get<T>() where T : notnull => _fixture.Services.GetRequiredService<T>();

    private async Task<StorageLocationDto> LocationAsync(Guid warehouseId, string code) =>
        (await Get<CreateStorageLocation.Handler>().HandleAsync(new CreateStorageLocation.Command(warehouseId, code, null, null, null), Ct)).Value;

    private async Task<decimal> QuantityAsync(Guid productId, Guid locationId)
    {
        var overview = await Get<ListStockOverview.Handler>().HandleAsync(new ListStockOverview.Query(), Ct);
        return overview.Rows.Where(x => x.ProductId == productId && x.LocationCode == LocationCodeOf(locationId)).Sum(x => x.Quantity);
    }

    private string LocationCodeOf(Guid locationId)
    {
        using var db = Get<Microsoft.EntityFrameworkCore.IDbContextFactory<Persistence.InventoryDbContext>>().CreateDbContext();
        return db.StorageLocations.Single(x => x.Id == locationId).Code;
    }

    private Task<Dotnetstore.PersonalApp.Shared.Results.Result<StockMovementDto>> MoveAsync(Guid productId, Guid locationId, string type, decimal quantity) =>
        Get<CreateStockMovement.Handler>().HandleAsync(new CreateStockMovement.Command(Today, productId, locationId, type, quantity, null, "test"), Ct);

    [Fact]
    public async Task MovementsAddUpToTheStockAtALocation()
    {
        var warehouse = await _fixture.MakeWarehouseAsync(Ct);
        var location = await LocationAsync(warehouse.Id, "A1");
        var product = Guid.NewGuid();

        (await MoveAsync(product, location.Id, "Receipt", 10m)).IsSuccess.ShouldBeTrue();
        (await MoveAsync(product, location.Id, "Issue", 4m)).IsSuccess.ShouldBeTrue();

        (await QuantityAsync(product, location.Id)).ShouldBe(6m);
    }

    [Fact]
    public async Task AnIssueCannotTakeMoreThanIsInStock()
    {
        var warehouse = await _fixture.MakeWarehouseAsync(Ct);
        var location = await LocationAsync(warehouse.Id, "A1");
        var product = Guid.NewGuid();
        await MoveAsync(product, location.Id, "Receipt", 3m);

        var result = await MoveAsync(product, location.Id, "Issue", 5m);

        result.Error.Code.ShouldBe("stock_movement_insufficient_stock");
        (await QuantityAsync(product, location.Id)).ShouldBe(3m);
    }

    [Fact]
    public async Task ATransferMovesStockBetweenLocationsAndReportsWhatIsLeft()
    {
        var warehouse = await _fixture.MakeWarehouseAsync(Ct);
        var from = await LocationAsync(warehouse.Id, "A1");
        var to = await LocationAsync(warehouse.Id, "B1");
        var product = Guid.NewGuid();
        await MoveAsync(product, from.Id, "Receipt", 10m);

        var result = await Get<TransferStock.Handler>().HandleAsync(new TransferStock.Command(product, from.Id, to.Id, 4m, Today, "move"), Ct);

        result.Value.ShouldBe(6m);
        (await QuantityAsync(product, from.Id)).ShouldBe(6m);
        (await QuantityAsync(product, to.Id)).ShouldBe(4m);
    }

    [Fact]
    public async Task ATransferNeedsTwoLocationsAndEnoughStock()
    {
        var warehouse = await _fixture.MakeWarehouseAsync(Ct);
        var from = await LocationAsync(warehouse.Id, "A1");
        var to = await LocationAsync(warehouse.Id, "B1");
        var product = Guid.NewGuid();
        await MoveAsync(product, from.Id, "Receipt", 2m);
        var handler = Get<TransferStock.Handler>();

        (await handler.HandleAsync(new TransferStock.Command(product, from.Id, from.Id, 1m, Today, null), Ct)).Error.Code.ShouldBe("stock_transfer_same_location");
        (await handler.HandleAsync(new TransferStock.Command(product, from.Id, to.Id, 0m, Today, null), Ct)).Error.Code.ShouldBe("stock_transfer_quantity");
        (await handler.HandleAsync(new TransferStock.Command(product, from.Id, to.Id, 9m, Today, null), Ct)).Error.Code.ShouldBe("stock_movement_insufficient_stock");
    }

    [Fact]
    public async Task ReceivingBooksStockAndPublishesGoodsReceivedPerLine()
    {
        var warehouse = await _fixture.MakeWarehouseAsync(Ct);
        var location = await LocationAsync(warehouse.Id, "A1");
        var product = Guid.NewGuid();
        var lineId = Guid.NewGuid();
        var handler = Get<OrderMovements.Handler>();

        var result = await handler.RecordAsync(
            new OrderMovements.Command(true, location.Id, "PO-1", Today, [new OrderMovements.Line(lineId, product, 100m, 7m)]), Ct);

        result.Value.ShouldBe(1);
        (await QuantityAsync(product, location.Id)).ShouldBe(7m);
        _fixture.Services.GetRequiredService<RecordingEventPublisher>().Published.OfType<GoodsReceived>()
            .ShouldContain(x => x.PurchaseOrderLineId == lineId && x.Quantity == 7m);
        (await handler.HandledAsync("PO-1", true, Ct))[product].ShouldBe(7m);
    }

    [Fact]
    public async Task IssuingChecksEveryLineBeforeAnythingIsSaved()
    {
        var warehouse = await _fixture.MakeWarehouseAsync(Ct);
        var location = await LocationAsync(warehouse.Id, "A1");
        var stocked = Guid.NewGuid();
        var missing = Guid.NewGuid();
        await MoveAsync(stocked, location.Id, "Receipt", 5m);

        var result = await Get<OrderMovements.Handler>().RecordAsync(
            new OrderMovements.Command(false, location.Id, "SO-1", Today,
                [new OrderMovements.Line(Guid.NewGuid(), stocked, 100m, 2m), new OrderMovements.Line(Guid.NewGuid(), missing, 100m, 1m)]), Ct);

        result.Error.Code.ShouldBe("stock_movement_insufficient_stock");
        (await QuantityAsync(stocked, location.Id)).ShouldBe(5m);
    }

    [Fact]
    public async Task AMovementWithNoQuantityIsRejected()
    {
        var warehouse = await _fixture.MakeWarehouseAsync(Ct);
        var location = await LocationAsync(warehouse.Id, "A1");

        var result = await Get<OrderMovements.Handler>().RecordAsync(
            new OrderMovements.Command(true, location.Id, "PO-1", Today, [new OrderMovements.Line(Guid.NewGuid(), Guid.NewGuid(), 100m, 0m)]), Ct);

        result.Error.Code.ShouldBe("goods_movement_nothing");
    }

    [Fact]
    public async Task ACountIsFilledFromStockCountedAndPostedAsAdjustments()
    {
        var warehouse = await _fixture.MakeWarehouseAsync(Ct);
        var location = await LocationAsync(warehouse.Id, "A1");
        var product = Guid.NewGuid();
        await MoveAsync(product, location.Id, "Receipt", 10m);
        var count = (await Get<CreateInventoryCount.Handler>()
            .HandleAsync(new CreateInventoryCount.Command("IC-1", warehouse.Id, Today, "Yearly"), Ct)).Value;

        var filled = await Get<FillInventoryCount.Handler>().HandleAsync(new FillInventoryCount.Command(count.Id), Ct);
        var refilled = await Get<FillInventoryCount.Handler>().HandleAsync(new FillInventoryCount.Command(count.Id), Ct);
        var line = (await Get<ListInventoryCountLines.Handler>().HandleAsync(new ListInventoryCountLines.Query(ParentId: count.Id), Ct)).Items.Single();
        await Get<UpdateInventoryCountLine.Handler>().HandleAsync(
            new UpdateInventoryCountLine.Command(line.Id, count.Id, line.ProductId, line.LocationId, line.ExpectedQty, 8m), Ct);
        var posted = await Get<PostInventoryCount.Handler>().HandleAsync(new PostInventoryCount.Command(count.Id), Ct);
        var postedAgain = await Get<PostInventoryCount.Handler>().HandleAsync(new PostInventoryCount.Command(count.Id), Ct);

        filled.Value.ShouldBe(1);
        refilled.Value.ShouldBe(0);
        line.ExpectedQty.ShouldBe(10m);
        posted.IsSuccess.ShouldBeTrue();
        (await QuantityAsync(product, location.Id)).ShouldBe(8m);
        postedAgain.Error.Code.ShouldBe("inventory_count_posted");
    }

    [Fact]
    public async Task TheOverviewCanShowOnlyLowStockAndOneWarehouse()
    {
        var warehouse = await _fixture.MakeWarehouseAsync(Ct);
        var other = await _fixture.MakeWarehouseAsync(Ct);
        var location = await LocationAsync(warehouse.Id, "A1");
        var elsewhere = await LocationAsync(other.Id, "B1");
        var low = Guid.NewGuid();
        var plenty = Guid.NewGuid();
        await MoveAsync(low, location.Id, "Receipt", 2m);
        await MoveAsync(plenty, location.Id, "Receipt", 50m);
        await MoveAsync(plenty, elsewhere.Id, "Receipt", 5m);

        var levels = await Get<Features.StockLevels.ListStockLevels.Handler>().HandleAsync(new Features.StockLevels.ListStockLevels.Query(), Ct);
        foreach (var level in levels.Items.Where(x => x.ProductId == low))
        {
            await Get<Features.StockLevels.UpdateStockLevel.Handler>().HandleAsync(
                new Features.StockLevels.UpdateStockLevel.Command(level.Id, level.ProductId, level.LocationId, 5m), Ct);
        }

        var onlyLow = await Get<ListStockOverview.Handler>().HandleAsync(new ListStockOverview.Query(OnlyLow: true), Ct);
        var oneWarehouse = await Get<ListStockOverview.Handler>().HandleAsync(new ListStockOverview.Query(WarehouseId: other.Id), Ct);

        onlyLow.Rows.Select(x => x.ProductId).ShouldBe([low]);
        oneWarehouse.Rows.Select(x => x.ProductId).ShouldBe([plenty]);
    }
}

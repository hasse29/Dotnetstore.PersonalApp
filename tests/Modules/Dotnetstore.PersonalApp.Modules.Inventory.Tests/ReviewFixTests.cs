using Dotnetstore.PersonalApp.Modules.Inventory.Consumers;
using Dotnetstore.PersonalApp.Modules.Inventory.Features;
using Dotnetstore.PersonalApp.Modules.Inventory.Features.Receiving;
using Dotnetstore.PersonalApp.Modules.Inventory.Features.StockLevels;
using Dotnetstore.PersonalApp.Modules.Inventory.Features.StockMovements;
using Dotnetstore.PersonalApp.Modules.Inventory.Features.StockOverview;
using Dotnetstore.PersonalApp.Modules.Inventory.Features.StorageLocations;
using Dotnetstore.PersonalApp.Modules.Inventory.Features.Warehouses;
using Dotnetstore.PersonalApp.Modules.Inventory.Persistence;
using Dotnetstore.PersonalApp.Shared.Contracts.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Dotnetstore.PersonalApp.Modules.Inventory.Tests;

public sealed class ReviewFixTests : IDisposable
{
    private static readonly DateOnly Today = new(2026, 1, 1);

    private readonly InventoryFixture _fixture = new();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public void Dispose() => _fixture.Dispose();

    private T Get<T>() where T : notnull => _fixture.Services.GetRequiredService<T>();

    private async Task<StorageLocationDto> LocationAsync(Guid warehouseId, string code) =>
        (await Get<CreateStorageLocation.Handler>().HandleAsync(new CreateStorageLocation.Command(warehouseId, code, null, null, null), Ct)).Value;

    private async Task<StockMovementDto> MoveAsync(Guid productId, Guid locationId, string type, decimal quantity) =>
        (await Get<CreateStockMovement.Handler>().HandleAsync(new CreateStockMovement.Command(Today, productId, locationId, type, quantity, null, null), Ct)).Value;

    private async Task<decimal> LevelAsync(Guid productId, Guid locationId)
    {
        await using var db = await Get<IDbContextFactory<InventoryDbContext>>().CreateDbContextAsync(Ct);
        return await db.StockLevels.Where(x => x.ProductId == productId && x.LocationId == locationId).Select(x => x.Quantity).SingleAsync(Ct);
    }

    [Fact]
    public async Task DeletingAReceiptThatLaterIssuesDependOnIsRefusedAndNothingChanges()
    {
        var location = await LocationAsync((await _fixture.MakeWarehouseAsync(Ct)).Id, "A1");
        var product = Guid.NewGuid();
        var receipt = await MoveAsync(product, location.Id, "Receipt", 10m);
        await MoveAsync(product, location.Id, "Issue", 10m);

        var result = await Get<DeleteStockMovement.Handler>().HandleAsync(new DeleteStockMovement.Command(receipt.Id), Ct);

        result.Error.Code.ShouldBe("stock_movement_insufficient_stock");
        (await LevelAsync(product, location.Id)).ShouldBe(0m);
        (await Get<ListStockMovements.Handler>().HandleAsync(new ListStockMovements.Query(), Ct)).Items.ShouldContain(x => x.Id == receipt.Id);
    }

    [Fact]
    public async Task EditingAReceiptBelowWhatWasIssuedIsRefusedAndNothingChanges()
    {
        var location = await LocationAsync((await _fixture.MakeWarehouseAsync(Ct)).Id, "A1");
        var product = Guid.NewGuid();
        var receipt = await MoveAsync(product, location.Id, "Receipt", 10m);
        await MoveAsync(product, location.Id, "Issue", 8m);

        var smaller = await Get<UpdateStockMovement.Handler>().HandleAsync(
            new UpdateStockMovement.Command(receipt.Id, Today, product, location.Id, "Receipt", 1m, null, null), Ct);
        var asIssue = await Get<UpdateStockMovement.Handler>().HandleAsync(
            new UpdateStockMovement.Command(receipt.Id, Today, product, location.Id, "Issue", 10m, null, null), Ct);

        smaller.Error.Code.ShouldBe("stock_movement_insufficient_stock");
        asIssue.Error.Code.ShouldBe("stock_movement_insufficient_stock");
        (await LevelAsync(product, location.Id)).ShouldBe(2m);
    }

    [Fact]
    public async Task MovingAReceiptToAnotherLocationIsRefusedWhenTheOldOneNeedsIt()
    {
        var warehouse = await _fixture.MakeWarehouseAsync(Ct);
        var a = await LocationAsync(warehouse.Id, "A1");
        var b = await LocationAsync(warehouse.Id, "B1");
        var product = Guid.NewGuid();
        var receipt = await MoveAsync(product, a.Id, "Receipt", 10m);
        await MoveAsync(product, a.Id, "Issue", 4m);

        var result = await Get<UpdateStockMovement.Handler>().HandleAsync(
            new UpdateStockMovement.Command(receipt.Id, Today, product, b.Id, "Receipt", 10m, null, null), Ct);

        result.Error.Code.ShouldBe("stock_movement_insufficient_stock");
        (await LevelAsync(product, a.Id)).ShouldBe(6m);
    }

    [Fact]
    public async Task OnlyTheMinimumOfAStockLevelCanBeEdited()
    {
        var location = await LocationAsync((await _fixture.MakeWarehouseAsync(Ct)).Id, "A1");
        var other = await LocationAsync((await _fixture.MakeWarehouseAsync(Ct)).Id, "B1");
        var product = Guid.NewGuid();
        await MoveAsync(product, location.Id, "Receipt", 10m);
        var level = (await Get<ListStockLevels.Handler>().HandleAsync(new ListStockLevels.Query(), Ct)).Items.Single();

        var moved = await Get<UpdateStockLevel.Handler>().HandleAsync(new UpdateStockLevel.Command(level.Id, product, other.Id, 3m), Ct);
        var minimum = await Get<UpdateStockLevel.Handler>().HandleAsync(new UpdateStockLevel.Command(level.Id, product, location.Id, 3m), Ct);

        moved.Error.Code.ShouldBe("stock_level_key_fixed");
        minimum.Value.MinQuantity.ShouldBe(3m);
        minimum.Value.Quantity.ShouldBe(10m);
    }

    [Fact]
    public async Task AWarehouseWithMovementsCannotBeDeleted()
    {
        var warehouse = await _fixture.MakeWarehouseAsync(Ct);
        var location = await LocationAsync(warehouse.Id, "A1");
        await MoveAsync(Guid.NewGuid(), location.Id, "Receipt", 1m);

        var result = await Get<DeleteWarehouse.Handler>().HandleAsync(new DeleteWarehouse.Command(warehouse.Id), Ct);

        result.Error.Code.ShouldBe("warehouse_in_use");
    }

    [Fact]
    public async Task ReceivingCannotGoPastTheOrderEvenWhenSubmittedTwice()
    {
        var location = await LocationAsync((await _fixture.MakeWarehouseAsync(Ct)).Id, "A1");
        var product = Guid.NewGuid();
        var command = new OrderMovements.Command(true, location.Id, "PO-1", Today, [new OrderMovements.Line(Guid.NewGuid(), product, 5m, 5m)]);
        var handler = Get<OrderMovements.Handler>();

        var first = await handler.RecordAsync(command, Ct);
        var second = await handler.RecordAsync(command, Ct);

        first.IsSuccess.ShouldBeTrue();
        second.Error.Code.ShouldBe("goods_movement_exceeds_order");
        (await LevelAsync(product, location.Id)).ShouldBe(5m);
    }

    [Fact]
    public async Task DeletingAProductKeepsItsHistoryButDropsItsLevels()
    {
        var location = await LocationAsync((await _fixture.MakeWarehouseAsync(Ct)).Id, "A1");
        var product = Guid.NewGuid();
        await MoveAsync(product, location.Id, "Receipt", 3m);

        await new ProductDeletedConsumer(Get<IDbContextFactory<InventoryDbContext>>()).ConsumeAsync(new ProductDeleted(product), Ct);

        (await Get<ListStockMovements.Handler>().HandleAsync(new ListStockMovements.Query(), Ct)).Items.ShouldContain(x => x.ProductId == product);
        (await Get<ListStockOverview.Handler>().HandleAsync(new ListStockOverview.Query(), Ct)).Rows.ShouldNotContain(x => x.ProductId == product);
    }

    [Fact]
    public void EveryMovementTypeSaysWhichWayItMovesStock()
    {
        StockLedger.Types.Order().ShouldBe(
            ["AdjustmentDown", "AdjustmentUp", "Issue", "Receipt", "Return", "Scrap", "TransferIn", "TransferOut"]);
        StockLedger.Types.Where(StockLedger.IsOutgoing).Order().ShouldBe(["AdjustmentDown", "Issue", "Scrap", "TransferOut"]);
    }
}

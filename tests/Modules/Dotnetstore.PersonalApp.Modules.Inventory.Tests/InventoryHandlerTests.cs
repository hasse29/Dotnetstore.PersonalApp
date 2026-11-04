using Dotnetstore.PersonalApp.Modules.Inventory.Features.InventoryCountLines;
using Dotnetstore.PersonalApp.Modules.Inventory.Features.InventoryCounts;
using Dotnetstore.PersonalApp.Modules.Inventory.Features.StockLevels;
using Dotnetstore.PersonalApp.Modules.Inventory.Features.StockMovements;
using Dotnetstore.PersonalApp.Modules.Inventory.Features.StorageLocations;
using Dotnetstore.PersonalApp.Modules.Inventory.Features.Warehouses;
using Microsoft.Extensions.DependencyInjection;

namespace Dotnetstore.PersonalApp.Modules.Inventory.Tests;

public sealed class InventoryHandlerTests : IDisposable
{
    private readonly InventoryFixture _fixture = new();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public void Dispose() => _fixture.Dispose();

    [Fact]
    public async Task WarehouseCanBeCreatedUpdatedListedAndDeleted()
    {
        var created = await _fixture.MakeWarehouseAsync(Ct);

        var updated = await _fixture.Services.GetRequiredService<UpdateWarehouse.Handler>()
            .HandleAsync(new UpdateWarehouse.Command(created.Id, created.Code, created.Name, created.Address, created.Active), Ct);
        var listed = await _fixture.Services.GetRequiredService<ListWarehouses.Handler>()
            .HandleAsync(new ListWarehouses.Query(), Ct);
        var deleted = await _fixture.Services.GetRequiredService<DeleteWarehouse.Handler>()
            .HandleAsync(new DeleteWarehouse.Command(created.Id), Ct);
        var deletedAgain = await _fixture.Services.GetRequiredService<DeleteWarehouse.Handler>()
            .HandleAsync(new DeleteWarehouse.Command(created.Id), Ct);

        updated.IsSuccess.ShouldBeTrue();
        listed.Items.ShouldContain(x => x.Id == created.Id);
        deleted.IsSuccess.ShouldBeTrue();
        deletedAgain.Error.Code.ShouldBe("warehouse_not_found");
    }

    [Fact]
    public async Task WarehouseRejectsAMissingCode()
    {
        var command = await _fixture.WarehouseCommandAsync(Ct);

        var result = await _fixture.Services.GetRequiredService<CreateWarehouse.Handler>()
            .HandleAsync(command with { Code = "  " }, Ct);

        result.Error.Code.ShouldBe("warehouse_code_required");
    }

    [Fact]
    public async Task WarehouseRejectsADuplicate()
    {
        var command = await _fixture.WarehouseCommandAsync(Ct);
        var handler = _fixture.Services.GetRequiredService<CreateWarehouse.Handler>();
        await handler.HandleAsync(command, Ct);

        var duplicate = await handler.HandleAsync(command, Ct);

        duplicate.Error.Code.ShouldBe("warehouse_conflict");
    }

    [Fact]
    public async Task WarehouseInUseCannotBeDeleted()
    {
        var parent = await _fixture.MakeWarehouseAsync(Ct);
        var childCommand = await _fixture.InventoryCountCommandAsync(Ct);
        await _fixture.Services.GetRequiredService<CreateInventoryCount.Handler>()
            .HandleAsync(childCommand with { WarehouseId = parent.Id }, Ct);

        var result = await _fixture.Services.GetRequiredService<DeleteWarehouse.Handler>()
            .HandleAsync(new DeleteWarehouse.Command(parent.Id), Ct);

        result.Error.Code.ShouldBe("warehouse_in_use");
    }

    [Fact]
    public async Task StorageLocationCanBeCreatedUpdatedListedAndDeleted()
    {
        var created = await _fixture.MakeStorageLocationAsync(Ct);

        var updated = await _fixture.Services.GetRequiredService<UpdateStorageLocation.Handler>()
            .HandleAsync(new UpdateStorageLocation.Command(created.Id, created.WarehouseId, created.Code, created.Zone, created.Description, created.Capacity), Ct);
        var listed = await _fixture.Services.GetRequiredService<ListStorageLocations.Handler>()
            .HandleAsync(new ListStorageLocations.Query(), Ct);
        var deleted = await _fixture.Services.GetRequiredService<DeleteStorageLocation.Handler>()
            .HandleAsync(new DeleteStorageLocation.Command(created.Id), Ct);
        var deletedAgain = await _fixture.Services.GetRequiredService<DeleteStorageLocation.Handler>()
            .HandleAsync(new DeleteStorageLocation.Command(created.Id), Ct);

        updated.IsSuccess.ShouldBeTrue();
        listed.Items.ShouldContain(x => x.Id == created.Id);
        deleted.IsSuccess.ShouldBeTrue();
        deletedAgain.Error.Code.ShouldBe("storage_location_not_found");
    }

    [Fact]
    public async Task StorageLocationRejectsAMissingCode()
    {
        var command = await _fixture.StorageLocationCommandAsync(Ct);

        var result = await _fixture.Services.GetRequiredService<CreateStorageLocation.Handler>()
            .HandleAsync(command with { Code = "  " }, Ct);

        result.Error.Code.ShouldBe("storage_location_code_required");
    }

    [Fact]
    public async Task StorageLocationRejectsADuplicate()
    {
        var command = await _fixture.StorageLocationCommandAsync(Ct);
        var handler = _fixture.Services.GetRequiredService<CreateStorageLocation.Handler>();
        await handler.HandleAsync(command, Ct);

        var duplicate = await handler.HandleAsync(command, Ct);

        duplicate.Error.Code.ShouldBe("storage_location_conflict");
    }

    [Fact]
    public async Task StorageLocationInUseCannotBeDeleted()
    {
        var parent = await _fixture.MakeStorageLocationAsync(Ct);
        var childCommand = await _fixture.StockMovementCommandAsync(Ct);
        await _fixture.Services.GetRequiredService<CreateStockMovement.Handler>()
            .HandleAsync(childCommand with { LocationId = parent.Id }, Ct);

        var result = await _fixture.Services.GetRequiredService<DeleteStorageLocation.Handler>()
            .HandleAsync(new DeleteStorageLocation.Command(parent.Id), Ct);

        result.Error.Code.ShouldBe("storage_location_in_use");
    }

    [Fact]
    public async Task StockLevelCanBeCreatedUpdatedListedAndDeleted()
    {
        var created = await _fixture.MakeStockLevelAsync(Ct);

        var updated = await _fixture.Services.GetRequiredService<UpdateStockLevel.Handler>()
            .HandleAsync(new UpdateStockLevel.Command(created.Id, created.ProductId, created.LocationId, created.MinQuantity), Ct);
        var listed = await _fixture.Services.GetRequiredService<ListStockLevels.Handler>()
            .HandleAsync(new ListStockLevels.Query(), Ct);
        var deleted = await _fixture.Services.GetRequiredService<DeleteStockLevel.Handler>()
            .HandleAsync(new DeleteStockLevel.Command(created.Id), Ct);
        var deletedAgain = await _fixture.Services.GetRequiredService<DeleteStockLevel.Handler>()
            .HandleAsync(new DeleteStockLevel.Command(created.Id), Ct);

        updated.IsSuccess.ShouldBeTrue();
        listed.Items.ShouldContain(x => x.Id == created.Id);
        deleted.IsSuccess.ShouldBeTrue();
        deletedAgain.Error.Code.ShouldBe("stock_level_not_found");
    }

    [Fact]
    public async Task StockLevelRejectsADuplicate()
    {
        var command = await _fixture.StockLevelCommandAsync(Ct);
        var handler = _fixture.Services.GetRequiredService<CreateStockLevel.Handler>();
        await handler.HandleAsync(command, Ct);

        var duplicate = await handler.HandleAsync(command, Ct);

        duplicate.Error.Code.ShouldBe("stock_level_conflict");
    }

    [Fact]
    public async Task StockMovementCanBeCreatedUpdatedListedAndDeleted()
    {
        var created = await _fixture.MakeStockMovementAsync(Ct);

        var updated = await _fixture.Services.GetRequiredService<UpdateStockMovement.Handler>()
            .HandleAsync(new UpdateStockMovement.Command(created.Id, created.Date, created.ProductId, created.LocationId, created.Type, created.Quantity, created.Reference, created.Note), Ct);
        var listed = await _fixture.Services.GetRequiredService<ListStockMovements.Handler>()
            .HandleAsync(new ListStockMovements.Query(), Ct);
        var deleted = await _fixture.Services.GetRequiredService<DeleteStockMovement.Handler>()
            .HandleAsync(new DeleteStockMovement.Command(created.Id), Ct);
        var deletedAgain = await _fixture.Services.GetRequiredService<DeleteStockMovement.Handler>()
            .HandleAsync(new DeleteStockMovement.Command(created.Id), Ct);

        updated.IsSuccess.ShouldBeTrue();
        listed.Items.ShouldContain(x => x.Id == created.Id);
        deleted.IsSuccess.ShouldBeTrue();
        deletedAgain.Error.Code.ShouldBe("stock_movement_not_found");
    }

    [Fact]
    public async Task InventoryCountCanBeCreatedUpdatedListedAndDeleted()
    {
        var created = await _fixture.MakeInventoryCountAsync(Ct);

        var updated = await _fixture.Services.GetRequiredService<UpdateInventoryCount.Handler>()
            .HandleAsync(new UpdateInventoryCount.Command(created.Id, created.Number, created.WarehouseId, created.Date, created.Note), Ct);
        var listed = await _fixture.Services.GetRequiredService<ListInventoryCounts.Handler>()
            .HandleAsync(new ListInventoryCounts.Query(), Ct);
        var deleted = await _fixture.Services.GetRequiredService<DeleteInventoryCount.Handler>()
            .HandleAsync(new DeleteInventoryCount.Command(created.Id), Ct);
        var deletedAgain = await _fixture.Services.GetRequiredService<DeleteInventoryCount.Handler>()
            .HandleAsync(new DeleteInventoryCount.Command(created.Id), Ct);

        updated.IsSuccess.ShouldBeTrue();
        listed.Items.ShouldContain(x => x.Id == created.Id);
        deleted.IsSuccess.ShouldBeTrue();
        deletedAgain.Error.Code.ShouldBe("inventory_count_not_found");
    }

    [Fact]
    public async Task InventoryCountRejectsADuplicate()
    {
        var command = await _fixture.InventoryCountCommandAsync(Ct);
        var handler = _fixture.Services.GetRequiredService<CreateInventoryCount.Handler>();
        await handler.HandleAsync(command, Ct);

        var duplicate = await handler.HandleAsync(command, Ct);

        duplicate.Error.Code.ShouldBe("inventory_count_conflict");
    }

    [Fact]
    public async Task InventoryCountLineCanBeCreatedUpdatedListedAndDeleted()
    {
        var created = await _fixture.MakeInventoryCountLineAsync(Ct);

        var updated = await _fixture.Services.GetRequiredService<UpdateInventoryCountLine.Handler>()
            .HandleAsync(new UpdateInventoryCountLine.Command(created.Id, created.InventoryCountId, created.ProductId, created.LocationId, created.ExpectedQty, created.CountedQty), Ct);
        var listed = await _fixture.Services.GetRequiredService<ListInventoryCountLines.Handler>()
            .HandleAsync(new ListInventoryCountLines.Query(), Ct);
        var deleted = await _fixture.Services.GetRequiredService<DeleteInventoryCountLine.Handler>()
            .HandleAsync(new DeleteInventoryCountLine.Command(created.Id), Ct);
        var deletedAgain = await _fixture.Services.GetRequiredService<DeleteInventoryCountLine.Handler>()
            .HandleAsync(new DeleteInventoryCountLine.Command(created.Id), Ct);

        updated.IsSuccess.ShouldBeTrue();
        listed.Items.ShouldContain(x => x.Id == created.Id);
        deleted.IsSuccess.ShouldBeTrue();
        deletedAgain.Error.Code.ShouldBe("inventory_count_line_not_found");
    }

    [Fact]
    public async Task InventoryCountLineRejectsADuplicate()
    {
        var command = await _fixture.InventoryCountLineCommandAsync(Ct);
        var handler = _fixture.Services.GetRequiredService<CreateInventoryCountLine.Handler>();
        await handler.HandleAsync(command, Ct);

        var duplicate = await handler.HandleAsync(command, Ct);

        duplicate.Error.Code.ShouldBe("inventory_count_line_conflict");
    }
}
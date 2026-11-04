using Dotnetstore.PersonalApp.Modules.Inventory.Features.InventoryCountLines;
using Dotnetstore.PersonalApp.Modules.Inventory.Features.InventoryCounts;
using Dotnetstore.PersonalApp.Modules.Inventory.Features.StockLevels;
using Dotnetstore.PersonalApp.Modules.Inventory.Features.StockMovements;
using Dotnetstore.PersonalApp.Modules.Inventory.Features.StorageLocations;
using Dotnetstore.PersonalApp.Modules.Inventory.Features.Warehouses;
using Dotnetstore.PersonalApp.Modules.Inventory.Persistence;
using Dotnetstore.PersonalApp.Shared.Events;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Dotnetstore.PersonalApp.Modules.Inventory.Tests;

/// <summary>
/// Real SQLite (in-memory, kept alive by one open connection) with the module's handlers resolved from a service provider.
/// </summary>
internal sealed class InventoryFixture : IDisposable
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private int _counter;

    public InventoryFixture()
    {
        _connection.Open();
        var options = new DbContextOptionsBuilder<InventoryDbContext>().UseSqlite(_connection).Options;

        var services = new ServiceCollection();
        services.AddSingleton(TimeProvider.System);
        Dotnetstore.PersonalApp.Shared.Localization.Localizer.Instance.AddResources(Dotnetstore.PersonalApp.Modules.Inventory.Resources.InventoryStrings.ResourceManager);
        services.AddSingleton<Dotnetstore.PersonalApp.Shared.Localization.ILocalizationService>(Dotnetstore.PersonalApp.Shared.Localization.Localizer.Instance);
        services.AddSingleton<RecordingEventPublisher>();
        services.AddSingleton<IEventPublisher>(sp => sp.GetRequiredService<RecordingEventPublisher>());
        services.AddSingleton<IDbContextFactory<InventoryDbContext>>(new TestDbFactory(options));
        InventoryModule.AddFeatures(services);
        Services = services.BuildServiceProvider();

        using var db = Services.GetRequiredService<IDbContextFactory<InventoryDbContext>>().CreateDbContext();
        db.Database.EnsureCreated();
    }

    public ServiceProvider Services { get; }

    public Task<CreateWarehouse.Command> WarehouseCommandAsync(CancellationToken cancellationToken)
    {
        var n = ++_counter;
        return Task.FromResult(new CreateWarehouse.Command(
            $"Warehouse{n}",
            $"Warehouse{n}",
            $"Warehouse{n}",
            false));
    }

    public async Task<WarehouseDto> MakeWarehouseAsync(CancellationToken cancellationToken) =>
        (await Services.GetRequiredService<CreateWarehouse.Handler>()
            .HandleAsync(await WarehouseCommandAsync(cancellationToken), cancellationToken)).Value;

    public async Task<CreateStorageLocation.Command> StorageLocationCommandAsync(CancellationToken cancellationToken)
    {
        var n = ++_counter;
        return new CreateStorageLocation.Command(
            (await MakeWarehouseAsync(cancellationToken)).Id,
            $"Storage{n}",
            $"Storage{n}",
            $"Storage{n}",
            1m);
    }

    public async Task<StorageLocationDto> MakeStorageLocationAsync(CancellationToken cancellationToken) =>
        (await Services.GetRequiredService<CreateStorageLocation.Handler>()
            .HandleAsync(await StorageLocationCommandAsync(cancellationToken), cancellationToken)).Value;

    public async Task<CreateStockLevel.Command> StockLevelCommandAsync(CancellationToken cancellationToken)
    {
        var n = ++_counter;
        return new CreateStockLevel.Command(
            Guid.NewGuid(),
            (await MakeStorageLocationAsync(cancellationToken)).Id,
            1m);
    }

    public async Task<StockLevelDto> MakeStockLevelAsync(CancellationToken cancellationToken) =>
        (await Services.GetRequiredService<CreateStockLevel.Handler>()
            .HandleAsync(await StockLevelCommandAsync(cancellationToken), cancellationToken)).Value;

    public async Task<CreateStockMovement.Command> StockMovementCommandAsync(CancellationToken cancellationToken)
    {
        var n = ++_counter;
        return new CreateStockMovement.Command(
            new DateOnly(2026, 1, 1),
            Guid.NewGuid(),
            (await MakeStorageLocationAsync(cancellationToken)).Id,
            "Receipt",
            1m,
            $"Stock{n}",
            $"Stock{n}");
    }

    public async Task<StockMovementDto> MakeStockMovementAsync(CancellationToken cancellationToken) =>
        (await Services.GetRequiredService<CreateStockMovement.Handler>()
            .HandleAsync(await StockMovementCommandAsync(cancellationToken), cancellationToken)).Value;

    public async Task<CreateInventoryCount.Command> InventoryCountCommandAsync(CancellationToken cancellationToken)
    {
        var n = ++_counter;
        return new CreateInventoryCount.Command(
            $"Inventory{n}",
            (await MakeWarehouseAsync(cancellationToken)).Id,
            new DateOnly(2026, 1, 1),
            $"Inventory{n}");
    }

    public async Task<InventoryCountDto> MakeInventoryCountAsync(CancellationToken cancellationToken) =>
        (await Services.GetRequiredService<CreateInventoryCount.Handler>()
            .HandleAsync(await InventoryCountCommandAsync(cancellationToken), cancellationToken)).Value;

    public async Task<CreateInventoryCountLine.Command> InventoryCountLineCommandAsync(CancellationToken cancellationToken)
    {
        var n = ++_counter;
        return new CreateInventoryCountLine.Command(
            (await MakeInventoryCountAsync(cancellationToken)).Id,
            Guid.NewGuid(),
            (await MakeStorageLocationAsync(cancellationToken)).Id,
            1m,
            1m);
    }

    public async Task<InventoryCountLineDto> MakeInventoryCountLineAsync(CancellationToken cancellationToken) =>
        (await Services.GetRequiredService<CreateInventoryCountLine.Handler>()
            .HandleAsync(await InventoryCountLineCommandAsync(cancellationToken), cancellationToken)).Value;

    public void Dispose()
    {
        Services.Dispose();
        _connection.Dispose();
    }

    private sealed class TestDbFactory(DbContextOptions<InventoryDbContext> options) : IDbContextFactory<InventoryDbContext>
    {
        public InventoryDbContext CreateDbContext() => new(options);
    }
}
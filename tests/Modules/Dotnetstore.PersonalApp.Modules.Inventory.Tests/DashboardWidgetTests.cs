using Dotnetstore.PersonalApp.Modules.Inventory.Dashboard;
using Dotnetstore.PersonalApp.Modules.Inventory.Features.StockLevels;
using Dotnetstore.PersonalApp.Modules.Inventory.Features.StockMovements;
using Dotnetstore.PersonalApp.Modules.Inventory.Features.StorageLocations;
using Dotnetstore.PersonalApp.Shared.Dashboard;
using Microsoft.Extensions.DependencyInjection;

namespace Dotnetstore.PersonalApp.Modules.Inventory.Tests;

public sealed class DashboardWidgetTests : IDisposable
{
    private readonly InventoryFixture _fixture = new();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public void Dispose() => _fixture.Dispose();

    private Task<DashboardPanel> LoadAsync() =>
        _fixture.Services.GetRequiredService<IDashboardWidget>().LoadAsync(new DashboardContext(Guid.NewGuid(), new DateOnly(2026, 1, 1)), Ct);

    [Fact]
    public async Task AnEmptyWarehouseShowsZeroesAndNoRows()
    {
        var panel = await LoadAsync();

        panel.Rows.ShouldBeEmpty();
        panel.IsEmpty.ShouldBeTrue();
        panel.Metrics.ShouldAllBe(m => !m.Warn);
    }

    [Fact]
    public async Task StockAtItsMinimumIsFlaggedAndListed()
    {
        var warehouse = await _fixture.MakeWarehouseAsync(Ct);
        var location = (await _fixture.Services.GetRequiredService<CreateStorageLocation.Handler>()
            .HandleAsync(new CreateStorageLocation.Command(warehouse.Id, "A1", null, null, null), Ct)).Value;
        var product = Guid.NewGuid();
        await _fixture.Services.GetRequiredService<CreateStockMovement.Handler>()
            .HandleAsync(new CreateStockMovement.Command(new DateOnly(2026, 1, 1), product, location.Id, "Receipt", 2m, null, null), Ct);
        var level = (await _fixture.Services.GetRequiredService<ListStockLevels.Handler>().HandleAsync(new ListStockLevels.Query(), Ct)).Items.Single();
        await _fixture.Services.GetRequiredService<UpdateStockLevel.Handler>()
            .HandleAsync(new UpdateStockLevel.Command(level.Id, product, location.Id, 5m), Ct);

        var panel = await LoadAsync();

        panel.Rows.ShouldHaveSingleItem().Value.ShouldBe("2 / 5");
        panel.Metrics.Count(m => m.Warn).ShouldBe(1);
    }
}

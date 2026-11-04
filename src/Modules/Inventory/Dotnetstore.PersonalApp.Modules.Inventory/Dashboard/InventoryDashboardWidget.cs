using Dotnetstore.PersonalApp.Modules.Inventory.Features;
using Dotnetstore.PersonalApp.Modules.Inventory.Persistence;
using Dotnetstore.PersonalApp.Shared.Dashboard;
using Dotnetstore.PersonalApp.Shared.Localization;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Inventory.Dashboard;

/// <summary>How many products are in stock and which ones have fallen to their minimum.</summary>
internal sealed class InventoryDashboardWidget(IDbContextFactory<InventoryDbContext> dbFactory, ExternalReferences references, ILocalizationService localization) : IDashboardWidget
{
    private const int MaxRows = 6;

    public string ModuleKey => InventoryGroup.ModuleKey;

    public int Order => 40;

    public async Task<DashboardPanel> LoadAsync(DashboardContext context, CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        var warehouses = await db.Warehouses.CountAsync(x => x.Active, cancellationToken).ConfigureAwait(false);
        var levels = await db.StockLevels.AsNoTracking()
            .Select(x => new { x.ProductId, x.Quantity, x.MinQuantity, Location = x.Location.Code, Warehouse = x.Location.Warehouse.Name })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var names = await references.ProductNamesAsync(cancellationToken).ConfigureAwait(false);

        var low = levels.Where(x => x.MinQuantity is { } min && x.Quantity <= min).ToList();
        var culture = localization.Culture;

        return new DashboardPanel(
            localization.Get("InventoryDashboard_Title"),
            [
                new(localization.Get("InventoryDashboard_Warehouses"), warehouses.ToString(culture)),
                new(localization.Get("InventoryDashboard_Products"), levels.Where(x => x.Quantity > 0m).Select(x => x.ProductId).Distinct().Count().ToString(culture)),
                new(localization.Get("InventoryDashboard_Low"), low.Count.ToString(culture), Warn: low.Count > 0),
            ],
            localization.Get("InventoryDashboard_ListTitle"),
            [.. low
                .OrderBy(x => x.Quantity - x.MinQuantity!.Value)
                .Take(MaxRows)
                .Select(x => new DashboardRow(
                    names.GetValueOrDefault(x.ProductId, string.Empty),
                    $"{x.Warehouse} / {x.Location}",
                    $"{x.Quantity.ToString("0.##", culture)} / {x.MinQuantity!.Value.ToString("0.##", culture)}"))],
            localization.Get("InventoryDashboard_Empty"));
    }
}

using Dotnetstore.PersonalApp.Modules.Production.Persistence;
using Dotnetstore.PersonalApp.Shared.Dashboard;
using Dotnetstore.PersonalApp.Shared.Localization;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Production.Dashboard;

/// <summary>The size of the catalogue and the work orders still open, the ones due first at the top.</summary>
internal sealed class ProductionDashboardWidget(IDbContextFactory<ProductionDbContext> dbFactory, ILocalizationService localization) : IDashboardWidget
{
    private const int MaxRows = 6;

    public string ModuleKey => ProductionGroup.ModuleKey;

    public int Order => 10;

    public async Task<DashboardPanel> LoadAsync(DashboardContext context, CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        var products = await db.Products.CountAsync(cancellationToken).ConfigureAwait(false);
        var open = db.WorkOrders.AsNoTracking().Where(x => x.EndDate == null);
        var openCount = await open.CountAsync(cancellationToken).ConfigureAwait(false);
        var overdue = await open.CountAsync(x => x.DueDate < context.Today, cancellationToken).ConfigureAwait(false);
        var next = await open
            .OrderBy(x => x.DueDate)
            .Take(MaxRows)
            .Select(x => new { x.Number, Product = x.Product.Name, x.DueDate, x.OrderQty })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var culture = localization.Culture;

        return new DashboardPanel(
            localization.Get("ProductionDashboard_Title"),
            [
                new(localization.Get("ProductionDashboard_Products"), products.ToString(culture)),
                new(localization.Get("ProductionDashboard_Open"), openCount.ToString(culture)),
                new(localization.Get("ProductionDashboard_Overdue"), overdue.ToString(culture), Warn: overdue > 0),
            ],
            localization.Get("ProductionDashboard_ListTitle"),
            [.. next.Select(x => new DashboardRow($"{x.Number}  {x.Product}", x.DueDate.ToString("d", culture), $"× {x.OrderQty.ToString(culture)}"))],
            localization.Get("ProductionDashboard_Empty"));
    }
}

using Dotnetstore.PersonalApp.Modules.Sales.Persistence;
using Dotnetstore.PersonalApp.Shared.Dashboard;
using Dotnetstore.PersonalApp.Shared.Localization;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Sales.Dashboard;

/// <summary>Orders still to be shipped, what was sold this month, and the latest orders.</summary>
internal sealed class SalesDashboardWidget(IDbContextFactory<SalesDbContext> dbFactory, ILocalizationService localization) : IDashboardWidget
{
    private const int MaxRows = 6;

    public string ModuleKey => SalesGroup.ModuleKey;

    public int Order => 20;

    public async Task<DashboardPanel> LoadAsync(DashboardContext context, CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        var monthStart = new DateOnly(context.Today.Year, context.Today.Month, 1);
        var open = await db.SalesOrders.CountAsync(x => x.Status == "I" || x.Status == "A" || x.Status == "B", cancellationToken).ConfigureAwait(false);
        var customers = await db.Customers.CountAsync(cancellationToken).ConfigureAwait(false);

        // SQLite cannot add decimals, so the totals for the month are added up here.
        var thisMonth = await db.SalesOrders.AsNoTracking()
            .Where(x => x.OrderDate >= monthStart && x.Status != "C" && x.Status != "R")
            .Select(x => x.TotalDue)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var latest = await db.SalesOrders.AsNoTracking()
            .OrderByDescending(x => x.OrderDate).ThenByDescending(x => x.Number)
            .Take(MaxRows)
            .Select(x => new { x.Number, Customer = x.Customer.Name, x.OrderDate, x.TotalDue })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var culture = localization.Culture;

        return new DashboardPanel(
            localization.Get("SalesDashboard_Title"),
            [
                new(localization.Get("SalesDashboard_Open"), open.ToString(culture)),
                new(localization.Get("SalesDashboard_Month"), thisMonth.Sum().ToString("N0", culture)),
                new(localization.Get("SalesDashboard_Customers"), customers.ToString(culture)),
            ],
            localization.Get("SalesDashboard_ListTitle"),
            [.. latest.Select(x => new DashboardRow($"{x.Number}  {x.Customer}", x.OrderDate.ToString("d", culture), x.TotalDue.ToString("N2", culture)))],
            localization.Get("SalesDashboard_Empty"));
    }
}

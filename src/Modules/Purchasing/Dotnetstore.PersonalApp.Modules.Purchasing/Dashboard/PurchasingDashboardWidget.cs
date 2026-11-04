using Dotnetstore.PersonalApp.Modules.Purchasing.Persistence;
using Dotnetstore.PersonalApp.Shared.Dashboard;
using Dotnetstore.PersonalApp.Shared.Localization;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Purchasing.Dashboard;

/// <summary>Orders waiting for approval or goods, what was ordered this month, and the latest orders.</summary>
internal sealed class PurchasingDashboardWidget(IDbContextFactory<PurchasingDbContext> dbFactory, ILocalizationService localization) : IDashboardWidget
{
    private const int MaxRows = 6;

    public string ModuleKey => PurchasingGroup.ModuleKey;

    public int Order => 30;

    public async Task<DashboardPanel> LoadAsync(DashboardContext context, CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        var monthStart = new DateOnly(context.Today.Year, context.Today.Month, 1);
        var pending = await db.PurchaseOrders.CountAsync(x => x.Status == "P", cancellationToken).ConfigureAwait(false);
        var approved = await db.PurchaseOrders.CountAsync(x => x.Status == "A", cancellationToken).ConfigureAwait(false);

        // SQLite cannot add decimals, so the totals for the month are added up here.
        var thisMonth = await db.PurchaseOrders.AsNoTracking()
            .Where(x => x.OrderDate >= monthStart && x.Status != "R")
            .Select(x => x.TotalDue)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var latest = await db.PurchaseOrders.AsNoTracking()
            .OrderByDescending(x => x.OrderDate).ThenByDescending(x => x.Number)
            .Take(MaxRows)
            .Select(x => new { x.Number, Vendor = x.Vendor.Name, x.OrderDate, x.TotalDue })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var culture = localization.Culture;

        return new DashboardPanel(
            localization.Get("PurchasingDashboard_Title"),
            [
                new(localization.Get("PurchasingDashboard_Pending"), pending.ToString(culture)),
                new(localization.Get("PurchasingDashboard_Approved"), approved.ToString(culture)),
                new(localization.Get("PurchasingDashboard_Month"), thisMonth.Sum().ToString("N0", culture)),
            ],
            localization.Get("PurchasingDashboard_ListTitle"),
            [.. latest.Select(x => new DashboardRow($"{x.Number}  {x.Vendor}", x.OrderDate.ToString("d", culture), x.TotalDue.ToString("N2", culture)))],
            localization.Get("PurchasingDashboard_Empty"));
    }
}

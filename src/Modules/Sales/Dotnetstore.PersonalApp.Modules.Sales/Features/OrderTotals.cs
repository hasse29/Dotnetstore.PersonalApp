using Dotnetstore.PersonalApp.Modules.Sales.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Sales.Features;

/// <summary>Keeps the totals on a sales order in step with its lines, tax and freight.</summary>
internal static class OrderTotals
{
    public static async Task RecalculateSalesOrderAsync(SalesDbContext db, Guid orderId, CancellationToken cancellationToken)
    {
        var order = await db.SalesOrders.FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken).ConfigureAwait(false);
        if (order is null)
        {
            return;
        }

        // SQLite cannot sum decimals, so the lines are added up here.
        var lineTotals = await db.SalesOrderDetails
            .Where(l => l.SalesOrderId == orderId)
            .Select(l => l.LineTotal)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        order.SubTotal = lineTotals.Sum();
        order.TotalDue = order.SubTotal + order.TaxAmt + order.Freight;

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

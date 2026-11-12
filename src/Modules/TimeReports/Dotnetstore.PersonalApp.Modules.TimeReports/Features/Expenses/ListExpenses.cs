using Dotnetstore.PersonalApp.Modules.TimeReports.Persistence;
using Dotnetstore.PersonalApp.Shared.Clients;
using Microsoft.EntityFrameworkCore;
using Dotnetstore.PersonalApp.Shared.Telemetry;

namespace Dotnetstore.PersonalApp.Modules.TimeReports.Features.Expenses;

/// <summary>Lists one user's expenses, newest first. The total covers every expense, not just the page.</summary>
internal static class ListExpenses
{
    public const int MaxPageSize = 50;

    public sealed record Query(Guid UserId, int Page = 1, int PageSize = MaxPageSize);

    public sealed record Response(IReadOnlyList<ExpenseDto> Items, int TotalCount, decimal TotalAmount);

    public sealed class Handler(IDbContextFactory<TimeReportsDbContext> dbFactory, IClientLookup clients)
    {
        public Task<Response> HandleAsync(Query query, CancellationToken cancellationToken) =>
            AppTelemetry.TraceAsync(this, () => HandleCoreAsync(query, cancellationToken), query);

        private async Task<Response> HandleCoreAsync(Query query, CancellationToken cancellationToken)
        {
            var page = Math.Max(query.Page, 1);
            var pageSize = Math.Clamp(query.PageSize, 1, MaxPageSize);

            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            var expenses = db.Expenses.AsNoTracking().Where(e => e.UserId == query.UserId);

            // SQLite cannot sum decimals in SQL, so the (small) set of amounts is summed here.
            var amounts = await expenses.Select(e => e.Amount).ToListAsync(cancellationToken).ConfigureAwait(false);

            var names = (await clients.ListAsync(cancellationToken).ConfigureAwait(false)).ToDictionary(c => c.Id, c => c.Name);

            // Projecting the file name only keeps the receipt bytes out of the list.
            var rows = await expenses
                .OrderByDescending(e => e.Date)
                .ThenByDescending(e => e.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(e => new { Expense = e, ReceiptFileName = e.Receipt != null ? e.Receipt.FileName : null })
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            var items = rows.ConvertAll(r => ExpenseDto.From(
                r.Expense,
                r.Expense.ClientId is { } id ? names.GetValueOrDefault(id) : null,
                r.ReceiptFileName));

            return new Response(items, amounts.Count, amounts.Sum());
        }
    }
}

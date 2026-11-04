using Dotnetstore.PersonalApp.Modules.TimeReports.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.TimeReports.Features.Expenses;

/// <summary>Reads back the receipt of one of the user's own expenses.</summary>
internal static class GetExpenseReceipt
{
    public sealed record Query(Guid ExpenseId, Guid UserId);

    public sealed class Handler(IDbContextFactory<TimeReportsDbContext> dbFactory)
    {
        public async Task<Result<ReceiptFile>> HandleAsync(Query query, CancellationToken cancellationToken)
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            var receipt = await db.Expenses.AsNoTracking()
                .Where(e => e.Id == query.ExpenseId && e.UserId == query.UserId && e.Receipt != null)
                .Select(e => new ReceiptFile(e.Receipt!.FileName, e.Receipt.Content))
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

            return receipt is null ? Error.NotFound("expense_receipt_not_found", "Receipt not found.") : receipt;
        }
    }
}

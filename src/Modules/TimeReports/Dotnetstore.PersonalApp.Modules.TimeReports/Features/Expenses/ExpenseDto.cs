using Dotnetstore.PersonalApp.Modules.TimeReports.Domain;

namespace Dotnetstore.PersonalApp.Modules.TimeReports.Features.Expenses;

/// <param name="ClientName">Null when there is no client, or it no longer exists.</param>
/// <param name="ReceiptFileName">Null when no receipt is attached.</param>
internal sealed record ExpenseDto(
    Guid Id,
    Guid? ClientId,
    string? ClientName,
    DateOnly Date,
    ExpenseCategory Category,
    string Description,
    decimal Amount,
    decimal VatAmount,
    string? ReceiptFileName)
{
    public bool HasReceipt => ReceiptFileName is not null;

    public static ExpenseDto From(Expense expense, string? clientName, string? receiptFileName) => new(
        expense.Id,
        expense.ClientId,
        clientName,
        expense.Date,
        expense.Category,
        expense.Description,
        expense.Amount,
        expense.VatAmount,
        receiptFileName);
}

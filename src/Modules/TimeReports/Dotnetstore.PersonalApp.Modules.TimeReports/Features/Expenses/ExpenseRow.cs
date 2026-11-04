using Dotnetstore.PersonalApp.Modules.TimeReports.Domain;

namespace Dotnetstore.PersonalApp.Modules.TimeReports.Features.Expenses;

/// <summary>An expense as the list shows it: the DTO plus its category in the current UI language.</summary>
internal sealed record ExpenseRow(ExpenseDto Expense, string CategoryLabel);

/// <summary>A category choice for the editor's picker.</summary>
internal sealed record ExpenseCategoryOption(ExpenseCategory Category, string Label);

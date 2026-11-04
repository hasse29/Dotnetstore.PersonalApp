using Dotnetstore.PersonalApp.Modules.TimeReports.Domain;
using Dotnetstore.PersonalApp.Modules.TimeReports.Persistence;
using Dotnetstore.PersonalApp.Shared.Clients;
using Dotnetstore.PersonalApp.Shared.Results;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.TimeReports.Features.Expenses;

/// <summary>
/// Changes one of the user's own expenses; someone else's is reported as not found. The receipt is kept unless
/// <see cref="Command.Receipt"/> replaces it or <see cref="Command.RemoveReceipt"/> deletes it.
/// </summary>
internal static class UpdateExpense
{
    public sealed record Command(
        Guid Id,
        Guid UserId,
        Guid? ClientId,
        DateOnly Date,
        ExpenseCategory Category,
        string Description,
        decimal Amount,
        decimal VatAmount,
        ReceiptFile? Receipt,
        bool RemoveReceipt);

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(c => c.UserId).UserIdRules();
            RuleFor(c => c.Date).DateRules();
            RuleFor(c => c.Category).CategoryRules();
            RuleFor(c => c.Description).DescriptionRules();
            RuleFor(c => c.Amount).AmountRules();
            RuleFor(c => c.VatAmount).VatRules(c => c.Amount);
            RuleFor(c => c.Receipt).ReceiptRules();
        }
    }

    public sealed class Handler(IDbContextFactory<TimeReportsDbContext> dbFactory, IValidator<Command> validator, IClientLookup clients)
    {
        public async Task<Result<ExpenseDto>> HandleAsync(Command command, CancellationToken cancellationToken)
        {
            var validation = await validator.ValidateAsync(command, cancellationToken).ConfigureAwait(false);
            if (!validation.IsValid)
            {
                return validation.ToError();
            }

            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            var expense = await db.Expenses.Include(e => e.Receipt)
                .FirstOrDefaultAsync(e => e.Id == command.Id && e.UserId == command.UserId, cancellationToken)
                .ConfigureAwait(false);
            if (expense is null)
            {
                return Error.NotFound("expense_not_found", "Expense not found.");
            }

            string? clientName = null;
            if (command.ClientId is { } clientId)
            {
                clientName = (await clients.ListAsync(cancellationToken).ConfigureAwait(false)).FirstOrDefault(c => c.Id == clientId)?.Name;
                if (clientName is null)
                {
                    return Error.NotFound("expense_client_not_found", "Client not found.");
                }
            }

            expense.ClientId = command.ClientId;
            expense.Date = command.Date;
            expense.Category = command.Category;
            expense.Description = command.Description.Trim();
            expense.Amount = ExpenseRules.Round(command.Amount);
            expense.VatAmount = ExpenseRules.Round(command.VatAmount);

            if (command.Receipt is { } receipt)
            {
                var fileName = ExpenseRules.ReceiptName(receipt.FileName);
                if (expense.Receipt is { } existing)
                {
                    existing.FileName = fileName;
                    existing.Content = receipt.Content;
                }
                else
                {
                    expense.Receipt = new ExpenseReceipt { FileName = fileName, Content = receipt.Content };
                }
            }
            else if (command.RemoveReceipt && expense.Receipt is { } toRemove)
            {
                db.ExpenseReceipts.Remove(toRemove);
                expense.Receipt = null;
            }

            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return ExpenseDto.From(expense, clientName, expense.Receipt?.FileName);
        }
    }
}

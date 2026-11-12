using Dotnetstore.PersonalApp.Modules.TimeReports.Domain;
using Dotnetstore.PersonalApp.Modules.TimeReports.Persistence;
using Dotnetstore.PersonalApp.Shared.Clients;
using Dotnetstore.PersonalApp.Shared.Results;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Dotnetstore.PersonalApp.Shared.Telemetry;

namespace Dotnetstore.PersonalApp.Modules.TimeReports.Features.Expenses;

internal static class CreateExpense
{
    public sealed record Command(
        Guid UserId,
        Guid? ClientId,
        DateOnly Date,
        ExpenseCategory Category,
        string Description,
        decimal Amount,
        decimal VatAmount,
        ReceiptFile? Receipt);

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

    public sealed class Handler(
        IDbContextFactory<TimeReportsDbContext> dbFactory,
        IValidator<Command> validator,
        IClientLookup clients,
        TimeProvider clock)
    {
        public Task<Result<ExpenseDto>> HandleAsync(Command command, CancellationToken cancellationToken) =>
            AppTelemetry.TraceAsync(this, () => HandleCoreAsync(command, cancellationToken), command);

        private async Task<Result<ExpenseDto>> HandleCoreAsync(Command command, CancellationToken cancellationToken)
        {
            var validation = await validator.ValidateAsync(command, cancellationToken).ConfigureAwait(false);
            if (!validation.IsValid)
            {
                return validation.ToError();
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

            var expense = new Expense
            {
                Id = Guid.CreateVersion7(),
                UserId = command.UserId,
                ClientId = command.ClientId,
                Date = command.Date,
                Category = command.Category,
                Description = command.Description.Trim(),
                Amount = ExpenseRules.Round(command.Amount),
                VatAmount = ExpenseRules.Round(command.VatAmount),
                CreatedAt = clock.GetUtcNow(),
            };

            if (command.Receipt is { } receipt)
            {
                expense.Receipt = new ExpenseReceipt { FileName = ExpenseRules.ReceiptName(receipt.FileName), Content = receipt.Content };
            }

            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            db.Expenses.Add(expense);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return ExpenseDto.From(expense, clientName, expense.Receipt?.FileName);
        }
    }
}

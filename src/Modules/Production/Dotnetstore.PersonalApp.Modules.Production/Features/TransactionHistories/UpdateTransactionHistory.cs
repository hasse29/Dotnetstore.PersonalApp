using Dotnetstore.PersonalApp.Modules.Production.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Production.Features.TransactionHistories;

internal static class UpdateTransactionHistory
{
    public sealed record Command(Guid Id, Guid ProductId, string? ReferenceOrder, DateOnly TransactionDate, string TransactionType, int Quantity, decimal ActualCost);

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(c => c.ProductId).NotEqual(Guid.Empty).WithErrorCode("transaction_history_product_required").WithMessage("Product is required.");
            RuleFor(c => c.ReferenceOrder).MaximumLength(25).WithErrorCode("transaction_history_reference_order_too_long").WithMessage("Reference order is too long.");
            RuleFor(c => c.TransactionType).Must(v => v is "W" or "S" or "P").WithErrorCode("transaction_history_transaction_type_invalid").WithMessage("Transaction type is not valid.");
            RuleFor(c => c.Quantity).InclusiveBetween(-1000000, 1000000).WithErrorCode("transaction_history_quantity_out_of_range").WithMessage("Quantity is out of range.");
            RuleFor(c => c.ActualCost).InclusiveBetween(0m, 1000000000m).WithErrorCode("transaction_history_actual_cost_out_of_range").WithMessage("Actual cost is out of range.");
        }
    }

    public sealed class Handler(IDbContextFactory<ProductionDbContext> dbFactory, IValidator<Command> validator)
    {
        public async Task<Result<TransactionHistoryDto>> HandleAsync(Command command, CancellationToken cancellationToken)
        {
            var validation = await validator.ValidateAsync(command, cancellationToken).ConfigureAwait(false);
            if (!validation.IsValid)
            {
                return validation.ToError();
            }

            var referenceOrderValue = ProductionRules.NormalizeOptional(command.ReferenceOrder);

            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            var entity = await db.TransactionHistories.FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken).ConfigureAwait(false);
            if (entity is null)
            {
                return Error.NotFound("transaction_history_not_found", "The record was not found.");
            }

            if (!await db.Products.AnyAsync(x => x.Id == command.ProductId, cancellationToken).ConfigureAwait(false))
            {
                return Error.NotFound("transaction_history_product_not_found", "Product not found.");
            }



            entity.ProductId = command.ProductId;
            entity.ReferenceOrder = referenceOrderValue;
            entity.TransactionDate = command.TransactionDate;
            entity.TransactionType = command.TransactionType;
            entity.Quantity = command.Quantity;
            entity.ActualCost = command.ActualCost;

            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return await db.TransactionHistories.AsNoTracking()
                .Where(x => x.Id == entity.Id)
                .Select(TransactionHistoryDto.Projection)
                .FirstAsync(cancellationToken)
                .ConfigureAwait(false);
        }
    }
}
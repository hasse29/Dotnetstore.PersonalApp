using Dotnetstore.PersonalApp.Modules.Inventory.Domain;
using Dotnetstore.PersonalApp.Modules.Inventory.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Inventory.Features.StockMovements;

internal static class CreateStockMovement
{
    public sealed record Command(DateOnly Date, Guid ProductId, Guid LocationId, string Type, decimal Quantity, string? Reference, string? Note);

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(c => c.ProductId).NotEqual(Guid.Empty).WithErrorCode("stock_movement_product_required").WithMessage("Product is required.");
            RuleFor(c => c.LocationId).NotEqual(Guid.Empty).WithErrorCode("stock_movement_location_required").WithMessage("Location is required.");
            RuleFor(c => c.Type).Must(v => v is "Receipt" or "Return" or "TransferIn" or "AdjustmentUp" or "Issue" or "Scrap" or "TransferOut" or "AdjustmentDown").WithErrorCode("stock_movement_type_invalid").WithMessage("Type is not valid.");
            RuleFor(c => c.Quantity).InclusiveBetween(0.0001m, 1000000000m).WithErrorCode("stock_movement_quantity_out_of_range").WithMessage("Quantity is out of range.");
            RuleFor(c => c.Reference).MaximumLength(100).WithErrorCode("stock_movement_reference_too_long").WithMessage("Reference is too long.");
            RuleFor(c => c.Note).MaximumLength(500).WithErrorCode("stock_movement_note_too_long").WithMessage("Note is too long.");
        }
    }

    public sealed class Handler(IDbContextFactory<InventoryDbContext> dbFactory, IValidator<Command> validator, ExternalReferences references)
    {
        public async Task<Result<StockMovementDto>> HandleAsync(Command command, CancellationToken cancellationToken)
        {
            var validation = await validator.ValidateAsync(command, cancellationToken).ConfigureAwait(false);
            if (!validation.IsValid)
            {
                return validation.ToError();
            }

            var referenceValue = InventoryRules.NormalizeOptional(command.Reference);
            var noteValue = InventoryRules.NormalizeOptional(command.Note);

            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);


            if (!await references.ProductExistsAsync(command.ProductId, cancellationToken).ConfigureAwait(false))
            {
                return Error.NotFound("stock_movement_product_not_found", "Product not found.");
            }

            if (!await db.StorageLocations.AnyAsync(x => x.Id == command.LocationId, cancellationToken).ConfigureAwait(false))
            {
                return Error.NotFound("stock_movement_location_not_found", "Location not found.");
            }



            var entity = new StockMovement
            {
                Id = Guid.CreateVersion7(),
                Date = command.Date,
                ProductId = command.ProductId,
                LocationId = command.LocationId,
                Type = command.Type,
                Quantity = command.Quantity,
                Reference = referenceValue,
                Note = noteValue,
            };

            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

            db.StockMovements.Add(entity);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            await StockLedger.RecalculateAsync(db, entity.ProductId, entity.LocationId, cancellationToken).ConfigureAwait(false);

            if (await StockLedger.GuardAsync(db, entity.ProductId, entity.LocationId, cancellationToken).ConfigureAwait(false) is { } guardError)
            {
                return guardError;
            }

            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

            var dto = await db.StockMovements.AsNoTracking()
                .Where(x => x.Id == entity.Id)
                .Select(StockMovementDto.Projection)
                .FirstAsync(cancellationToken)
                .ConfigureAwait(false);

            return dto.WithNames(await references.ProductNamesAsync(cancellationToken).ConfigureAwait(false));
        }
    }
}
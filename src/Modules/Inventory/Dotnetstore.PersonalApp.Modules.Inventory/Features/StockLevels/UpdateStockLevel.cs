using Dotnetstore.PersonalApp.Modules.Inventory.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Inventory.Features.StockLevels;

internal static class UpdateStockLevel
{
    public sealed record Command(Guid Id, Guid ProductId, Guid LocationId, decimal? MinQuantity);

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(c => c.ProductId).NotEqual(Guid.Empty).WithErrorCode("stock_level_product_required").WithMessage("Product is required.");
            RuleFor(c => c.LocationId).NotEqual(Guid.Empty).WithErrorCode("stock_level_location_required").WithMessage("Location is required.");
            RuleFor(c => c.MinQuantity).InclusiveBetween(0m, 1000000000m).WithErrorCode("stock_level_min_quantity_out_of_range").WithMessage("Reorder level is out of range.").When(c => c.MinQuantity is not null);
        }
    }

    public sealed class Handler(IDbContextFactory<InventoryDbContext> dbFactory, IValidator<Command> validator, ExternalReferences references)
    {
        public async Task<Result<StockLevelDto>> HandleAsync(Command command, CancellationToken cancellationToken)
        {
            var validation = await validator.ValidateAsync(command, cancellationToken).ConfigureAwait(false);
            if (!validation.IsValid)
            {
                return validation.ToError();
            }



            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            var entity = await db.StockLevels.FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken).ConfigureAwait(false);
            if (entity is null)
            {
                return Error.NotFound("stock_level_not_found", "The record was not found.");
            }

            // The quantity follows the movements, so a row stays with its product and location; only the minimum is edited.
            if (command.ProductId != entity.ProductId || command.LocationId != entity.LocationId)
            {
                return Error.Validation("stock_level_key_fixed", "Only the minimum quantity can be changed.");
            }


            if (!await references.ProductExistsAsync(command.ProductId, cancellationToken).ConfigureAwait(false))
            {
                return Error.NotFound("stock_level_product_not_found", "Product not found.");
            }

            if (!await db.StorageLocations.AnyAsync(x => x.Id == command.LocationId, cancellationToken).ConfigureAwait(false))
            {
                return Error.NotFound("stock_level_location_not_found", "Location not found.");
            }

            if (await db.StockLevels.AnyAsync(x => x.ProductId == command.ProductId && x.LocationId == command.LocationId && x.Id != command.Id, cancellationToken).ConfigureAwait(false))
            {
                return Error.Conflict("stock_level_conflict", "A stock level with the same values already exists.");
            }

            entity.ProductId = command.ProductId;
            entity.LocationId = command.LocationId;
            entity.MinQuantity = command.MinQuantity;

            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            var dto = await db.StockLevels.AsNoTracking()
                .Where(x => x.Id == entity.Id)
                .Select(StockLevelDto.Projection)
                .FirstAsync(cancellationToken)
                .ConfigureAwait(false);

            return dto.WithNames(await references.ProductNamesAsync(cancellationToken).ConfigureAwait(false));
        }
    }
}
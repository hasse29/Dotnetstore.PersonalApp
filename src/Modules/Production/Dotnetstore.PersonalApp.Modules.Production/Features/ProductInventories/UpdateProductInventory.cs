using Dotnetstore.PersonalApp.Modules.Production.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Production.Features.ProductInventories;

internal static class UpdateProductInventory
{
    public sealed record Command(Guid Id, Guid ProductId, Guid LocationId, string Shelf, int Bin, int Quantity);

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(c => c.ProductId).NotEqual(Guid.Empty).WithErrorCode("product_inventory_product_required").WithMessage("Product is required.");
            RuleFor(c => c.LocationId).NotEqual(Guid.Empty).WithErrorCode("product_inventory_location_required").WithMessage("Location is required.");
            RuleFor(c => c.Shelf).NotEmpty().WithErrorCode("product_inventory_shelf_required").WithMessage("Shelf is required.").MaximumLength(10).WithErrorCode("product_inventory_shelf_too_long").WithMessage("Shelf is too long.");
            RuleFor(c => c.Bin).InclusiveBetween(0, 100).WithErrorCode("product_inventory_bin_out_of_range").WithMessage("Bin is out of range.");
            RuleFor(c => c.Quantity).InclusiveBetween(0, 1000000).WithErrorCode("product_inventory_quantity_out_of_range").WithMessage("Quantity is out of range.");
        }
    }

    public sealed class Handler(IDbContextFactory<ProductionDbContext> dbFactory, IValidator<Command> validator)
    {
        public async Task<Result<ProductInventoryDto>> HandleAsync(Command command, CancellationToken cancellationToken)
        {
            var validation = await validator.ValidateAsync(command, cancellationToken).ConfigureAwait(false);
            if (!validation.IsValid)
            {
                return validation.ToError();
            }

            var shelfValue = command.Shelf.Trim();

            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            var entity = await db.ProductInventories.FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken).ConfigureAwait(false);
            if (entity is null)
            {
                return Error.NotFound("product_inventory_not_found", "The record was not found.");
            }

            if (!await db.Products.AnyAsync(x => x.Id == command.ProductId, cancellationToken).ConfigureAwait(false))
            {
                return Error.NotFound("product_inventory_product_not_found", "Product not found.");
            }

            if (!await db.Locations.AnyAsync(x => x.Id == command.LocationId, cancellationToken).ConfigureAwait(false))
            {
                return Error.NotFound("product_inventory_location_not_found", "Location not found.");
            }

            if (await db.ProductInventories.AnyAsync(x => x.ProductId == command.ProductId && x.LocationId == command.LocationId && x.Id != command.Id, cancellationToken).ConfigureAwait(false))
            {
                return Error.Conflict("product_inventory_conflict", "A inventory entry with the same values already exists.");
            }

            entity.ProductId = command.ProductId;
            entity.LocationId = command.LocationId;
            entity.Shelf = shelfValue;
            entity.Bin = command.Bin;
            entity.Quantity = command.Quantity;

            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return await db.ProductInventories.AsNoTracking()
                .Where(x => x.Id == entity.Id)
                .Select(ProductInventoryDto.Projection)
                .FirstAsync(cancellationToken)
                .ConfigureAwait(false);
        }
    }
}
using Dotnetstore.PersonalApp.Modules.Inventory.Domain;
using Dotnetstore.PersonalApp.Modules.Inventory.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Inventory.Features.InventoryCountLines;

internal static class CreateInventoryCountLine
{
    public sealed record Command(Guid InventoryCountId, Guid ProductId, Guid LocationId, decimal ExpectedQty, decimal? CountedQty);

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(c => c.InventoryCountId).NotEqual(Guid.Empty).WithErrorCode("inventory_count_line_inventory_count_required").WithMessage("Inventory count is required.");
            RuleFor(c => c.ProductId).NotEqual(Guid.Empty).WithErrorCode("inventory_count_line_product_required").WithMessage("Product is required.");
            RuleFor(c => c.LocationId).NotEqual(Guid.Empty).WithErrorCode("inventory_count_line_location_required").WithMessage("Location is required.");
            RuleFor(c => c.ExpectedQty).InclusiveBetween(0m, 1000000000m).WithErrorCode("inventory_count_line_expected_qty_out_of_range").WithMessage("Expected quantity is out of range.");
            RuleFor(c => c.CountedQty).InclusiveBetween(0m, 1000000000m).WithErrorCode("inventory_count_line_counted_qty_out_of_range").WithMessage("Counted quantity is out of range.").When(c => c.CountedQty is not null);
        }
    }

    public sealed class Handler(IDbContextFactory<InventoryDbContext> dbFactory, IValidator<Command> validator, ExternalReferences references)
    {
        public async Task<Result<InventoryCountLineDto>> HandleAsync(Command command, CancellationToken cancellationToken)
        {
            var validation = await validator.ValidateAsync(command, cancellationToken).ConfigureAwait(false);
            if (!validation.IsValid)
            {
                return validation.ToError();
            }



            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);


            if (!await db.InventoryCounts.AnyAsync(x => x.Id == command.InventoryCountId, cancellationToken).ConfigureAwait(false))
            {
                return Error.NotFound("inventory_count_line_inventory_count_not_found", "Inventory count not found.");
            }

            if (!await references.ProductExistsAsync(command.ProductId, cancellationToken).ConfigureAwait(false))
            {
                return Error.NotFound("inventory_count_line_product_not_found", "Product not found.");
            }

            if (!await db.StorageLocations.AnyAsync(x => x.Id == command.LocationId, cancellationToken).ConfigureAwait(false))
            {
                return Error.NotFound("inventory_count_line_location_not_found", "Location not found.");
            }

            if (await db.InventoryCountLines.AnyAsync(x => x.InventoryCountId == command.InventoryCountId && x.ProductId == command.ProductId && x.LocationId == command.LocationId, cancellationToken).ConfigureAwait(false))
            {
                return Error.Conflict("inventory_count_line_conflict", "A count line with the same values already exists.");
            }

            var entity = new InventoryCountLine
            {
                Id = Guid.CreateVersion7(),
                InventoryCountId = command.InventoryCountId,
                ProductId = command.ProductId,
                LocationId = command.LocationId,
                ExpectedQty = command.ExpectedQty,
                CountedQty = command.CountedQty,
                Difference = command.CountedQty is { } counted ? counted - command.ExpectedQty : 0m,
            };

            db.InventoryCountLines.Add(entity);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            var dto = await db.InventoryCountLines.AsNoTracking()
                .Where(x => x.Id == entity.Id)
                .Select(InventoryCountLineDto.Projection)
                .FirstAsync(cancellationToken)
                .ConfigureAwait(false);

            return dto.WithNames(await references.ProductNamesAsync(cancellationToken).ConfigureAwait(false));
        }
    }
}
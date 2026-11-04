using Dotnetstore.PersonalApp.Modules.Inventory.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Inventory.Features.StorageLocations;

internal static class UpdateStorageLocation
{
    public sealed record Command(Guid Id, Guid WarehouseId, string Code, string? Zone, string? Description, decimal? Capacity);

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(c => c.WarehouseId).NotEqual(Guid.Empty).WithErrorCode("storage_location_warehouse_required").WithMessage("Warehouse is required.");
            RuleFor(c => c.Code).NotEmpty().WithErrorCode("storage_location_code_required").WithMessage("Code is required.").MaximumLength(30).WithErrorCode("storage_location_code_too_long").WithMessage("Code is too long.");
            RuleFor(c => c.Zone).MaximumLength(50).WithErrorCode("storage_location_zone_too_long").WithMessage("Zone is too long.");
            RuleFor(c => c.Description).MaximumLength(200).WithErrorCode("storage_location_description_too_long").WithMessage("Description is too long.");
            RuleFor(c => c.Capacity).InclusiveBetween(0m, 1000000000m).WithErrorCode("storage_location_capacity_out_of_range").WithMessage("Capacity is out of range.").When(c => c.Capacity is not null);
        }
    }

    public sealed class Handler(IDbContextFactory<InventoryDbContext> dbFactory, IValidator<Command> validator)
    {
        public async Task<Result<StorageLocationDto>> HandleAsync(Command command, CancellationToken cancellationToken)
        {
            var validation = await validator.ValidateAsync(command, cancellationToken).ConfigureAwait(false);
            if (!validation.IsValid)
            {
                return validation.ToError();
            }

            var codeValue = command.Code.Trim();
            var zoneValue = InventoryRules.NormalizeOptional(command.Zone);
            var descriptionValue = InventoryRules.NormalizeOptional(command.Description);

            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            var entity = await db.StorageLocations.FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken).ConfigureAwait(false);
            if (entity is null)
            {
                return Error.NotFound("storage_location_not_found", "The record was not found.");
            }

            if (!await db.Warehouses.AnyAsync(x => x.Id == command.WarehouseId, cancellationToken).ConfigureAwait(false))
            {
                return Error.NotFound("storage_location_warehouse_not_found", "Warehouse not found.");
            }

            if (await db.StorageLocations.AnyAsync(x => x.Code == codeValue && x.Id != command.Id, cancellationToken).ConfigureAwait(false))
            {
                return Error.Conflict("storage_location_conflict", "A storage location with the same values already exists.");
            }

            entity.WarehouseId = command.WarehouseId;
            entity.Code = codeValue;
            entity.Zone = zoneValue;
            entity.Description = descriptionValue;
            entity.Capacity = command.Capacity;

            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return await db.StorageLocations.AsNoTracking()
                .Where(x => x.Id == entity.Id)
                .Select(StorageLocationDto.Projection)
                .FirstAsync(cancellationToken)
                .ConfigureAwait(false);
        }
    }
}
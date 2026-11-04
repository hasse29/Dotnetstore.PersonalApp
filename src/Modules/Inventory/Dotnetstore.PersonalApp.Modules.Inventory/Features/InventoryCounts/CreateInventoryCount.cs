using Dotnetstore.PersonalApp.Modules.Inventory.Domain;
using Dotnetstore.PersonalApp.Modules.Inventory.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Inventory.Features.InventoryCounts;

internal static class CreateInventoryCount
{
    public sealed record Command(string? Number, Guid WarehouseId, DateOnly Date, string? Note);

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(c => c.Number).MaximumLength(25).WithErrorCode("inventory_count_number_too_long").WithMessage("Number is too long.");
            RuleFor(c => c.WarehouseId).NotEqual(Guid.Empty).WithErrorCode("inventory_count_warehouse_required").WithMessage("Warehouse is required.");
            RuleFor(c => c.Note).MaximumLength(500).WithErrorCode("inventory_count_note_too_long").WithMessage("Note is too long.");
        }
    }

    public sealed class Handler(IDbContextFactory<InventoryDbContext> dbFactory, IValidator<Command> validator)
    {
        public async Task<Result<InventoryCountDto>> HandleAsync(Command command, CancellationToken cancellationToken)
        {
            var validation = await validator.ValidateAsync(command, cancellationToken).ConfigureAwait(false);
            if (!validation.IsValid)
            {
                return validation.ToError();
            }

            var numberValue = InventoryRules.NormalizeOptional(command.Number);
            var noteValue = InventoryRules.NormalizeOptional(command.Note);

            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            numberValue ??= await NextNumberAsync(db, cancellationToken).ConfigureAwait(false);

            if (!await db.Warehouses.AnyAsync(x => x.Id == command.WarehouseId, cancellationToken).ConfigureAwait(false))
            {
                return Error.NotFound("inventory_count_warehouse_not_found", "Warehouse not found.");
            }

            if (await db.InventoryCounts.AnyAsync(x => x.Number == numberValue, cancellationToken).ConfigureAwait(false))
            {
                return Error.Conflict("inventory_count_conflict", "A inventory count with the same values already exists.");
            }

            var entity = new InventoryCount
            {
                Id = Guid.CreateVersion7(),
                Number = numberValue,
                WarehouseId = command.WarehouseId,
                Date = command.Date,
                Note = noteValue,
            };

            db.InventoryCounts.Add(entity);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return await db.InventoryCounts.AsNoTracking()
                .Where(x => x.Id == entity.Id)
                .Select(InventoryCountDto.Projection)
                .FirstAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        private static async Task<string> NextNumberAsync(InventoryDbContext db, CancellationToken cancellationToken)
        {
            var count = await db.InventoryCounts.CountAsync(cancellationToken).ConfigureAwait(false);
            string candidate;
            do
            {
                count++;
                candidate = $"IC-{count:D6}";
            }
            while (await db.InventoryCounts.AnyAsync(x => x.Number == candidate, cancellationToken).ConfigureAwait(false));

            return candidate;
        }
    }
}
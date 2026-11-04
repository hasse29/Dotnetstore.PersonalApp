using Dotnetstore.PersonalApp.Modules.Inventory.Domain;
using Dotnetstore.PersonalApp.Modules.Inventory.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Inventory.Features.Warehouses;

internal static class CreateWarehouse
{
    public sealed record Command(string Code, string Name, string? Address, bool Active);

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(c => c.Code).NotEmpty().WithErrorCode("warehouse_code_required").WithMessage("Code is required.").MaximumLength(20).WithErrorCode("warehouse_code_too_long").WithMessage("Code is too long.");
            RuleFor(c => c.Name).NotEmpty().WithErrorCode("warehouse_name_required").WithMessage("Name is required.").MaximumLength(100).WithErrorCode("warehouse_name_too_long").WithMessage("Name is too long.");
            RuleFor(c => c.Address).MaximumLength(200).WithErrorCode("warehouse_address_too_long").WithMessage("Address is too long.");
        }
    }

    public sealed class Handler(IDbContextFactory<InventoryDbContext> dbFactory, IValidator<Command> validator)
    {
        public async Task<Result<WarehouseDto>> HandleAsync(Command command, CancellationToken cancellationToken)
        {
            var validation = await validator.ValidateAsync(command, cancellationToken).ConfigureAwait(false);
            if (!validation.IsValid)
            {
                return validation.ToError();
            }

            var codeValue = command.Code.Trim();
            var nameValue = command.Name.Trim();
            var addressValue = InventoryRules.NormalizeOptional(command.Address);

            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);




            if (await db.Warehouses.AnyAsync(x => x.Code == codeValue, cancellationToken).ConfigureAwait(false))
            {
                return Error.Conflict("warehouse_conflict", "A warehouse with the same values already exists.");
            }

            if (await db.Warehouses.AnyAsync(x => x.Name == nameValue, cancellationToken).ConfigureAwait(false))
            {
                return Error.Conflict("warehouse_conflict", "A warehouse with the same values already exists.");
            }

            var entity = new Warehouse
            {
                Id = Guid.CreateVersion7(),
                Code = codeValue,
                Name = nameValue,
                Address = addressValue,
                Active = command.Active,
            };

            db.Warehouses.Add(entity);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return await db.Warehouses.AsNoTracking()
                .Where(x => x.Id == entity.Id)
                .Select(WarehouseDto.Projection)
                .FirstAsync(cancellationToken)
                .ConfigureAwait(false);
        }
    }
}
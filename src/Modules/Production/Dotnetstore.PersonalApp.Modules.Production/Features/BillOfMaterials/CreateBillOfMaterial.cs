using Dotnetstore.PersonalApp.Modules.Production.Domain;
using Dotnetstore.PersonalApp.Modules.Production.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Production.Features.BillOfMaterials;

internal static class CreateBillOfMaterial
{
    public sealed record Command(Guid? ProductAssemblyId, Guid ComponentId, DateOnly StartDate, DateOnly? EndDate, Guid UnitId, int BomLevel, decimal PerAssemblyQty);

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(c => c.ComponentId).NotEqual(Guid.Empty).WithErrorCode("bill_of_material_component_required").WithMessage("Component is required.");
            RuleFor(c => c.UnitId).NotEqual(Guid.Empty).WithErrorCode("bill_of_material_unit_required").WithMessage("Unit is required.");
            RuleFor(c => c.BomLevel).InclusiveBetween(0, 100).WithErrorCode("bill_of_material_bom_level_out_of_range").WithMessage("BOM level is out of range.");
            RuleFor(c => c.PerAssemblyQty).InclusiveBetween(0m, 1000000000m).WithErrorCode("bill_of_material_per_assembly_qty_out_of_range").WithMessage("Quantity per assembly is out of range.");
            RuleFor(c => c).Must(c => c.EndDate is null || c.EndDate >= c.StartDate).WithErrorCode("bill_of_material_end_before_start").WithMessage("The end date is before the start date.");
        }
    }

    public sealed class Handler(IDbContextFactory<ProductionDbContext> dbFactory, IValidator<Command> validator)
    {
        public async Task<Result<BillOfMaterialDto>> HandleAsync(Command command, CancellationToken cancellationToken)
        {
            var validation = await validator.ValidateAsync(command, cancellationToken).ConfigureAwait(false);
            if (!validation.IsValid)
            {
                return validation.ToError();
            }



            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);


            if (command.ProductAssemblyId is { } productAssemblyId && !await db.Products.AnyAsync(x => x.Id == productAssemblyId, cancellationToken).ConfigureAwait(false))
            {
                return Error.NotFound("bill_of_material_product_assembly_not_found", "Assembly product not found.");
            }

            if (!await db.Products.AnyAsync(x => x.Id == command.ComponentId, cancellationToken).ConfigureAwait(false))
            {
                return Error.NotFound("bill_of_material_component_not_found", "Component not found.");
            }

            if (!await db.UnitMeasures.AnyAsync(x => x.Id == command.UnitId, cancellationToken).ConfigureAwait(false))
            {
                return Error.NotFound("bill_of_material_unit_not_found", "Unit not found.");
            }



            var entity = new BillOfMaterial
            {
                Id = Guid.CreateVersion7(),
                ProductAssemblyId = command.ProductAssemblyId,
                ComponentId = command.ComponentId,
                StartDate = command.StartDate,
                EndDate = command.EndDate,
                UnitId = command.UnitId,
                BomLevel = command.BomLevel,
                PerAssemblyQty = command.PerAssemblyQty,
            };

            db.BillOfMaterials.Add(entity);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return await db.BillOfMaterials.AsNoTracking()
                .Where(x => x.Id == entity.Id)
                .Select(BillOfMaterialDto.Projection)
                .FirstAsync(cancellationToken)
                .ConfigureAwait(false);
        }
    }
}
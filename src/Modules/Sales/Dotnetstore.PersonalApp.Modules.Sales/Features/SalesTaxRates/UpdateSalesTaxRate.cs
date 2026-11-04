using Dotnetstore.PersonalApp.Modules.Sales.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Sales.Features.SalesTaxRates;

internal static class UpdateSalesTaxRate
{
    public sealed record Command(Guid Id, string Name, string StateProvince, string TaxType, decimal TaxRate);

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(c => c.Name).NotEmpty().WithErrorCode("sales_tax_rate_name_required").WithMessage("Name is required.").MaximumLength(50).WithErrorCode("sales_tax_rate_name_too_long").WithMessage("Name is too long.");
            RuleFor(c => c.StateProvince).NotEmpty().WithErrorCode("sales_tax_rate_state_province_required").WithMessage("State or province is required.").MaximumLength(50).WithErrorCode("sales_tax_rate_state_province_too_long").WithMessage("State or province is too long.");
            RuleFor(c => c.TaxType).Must(v => v is "R" or "W" or "A").WithErrorCode("sales_tax_rate_tax_type_invalid").WithMessage("Tax type is not valid.");
            RuleFor(c => c.TaxRate).InclusiveBetween(0m, 100m).WithErrorCode("sales_tax_rate_tax_rate_out_of_range").WithMessage("Tax rate (%) is out of range.");
        }
    }

    public sealed class Handler(IDbContextFactory<SalesDbContext> dbFactory, IValidator<Command> validator)
    {
        public async Task<Result<SalesTaxRateDto>> HandleAsync(Command command, CancellationToken cancellationToken)
        {
            var validation = await validator.ValidateAsync(command, cancellationToken).ConfigureAwait(false);
            if (!validation.IsValid)
            {
                return validation.ToError();
            }

            var nameValue = command.Name.Trim();
            var stateProvinceValue = command.StateProvince.Trim();

            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            var entity = await db.SalesTaxRates.FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken).ConfigureAwait(false);
            if (entity is null)
            {
                return Error.NotFound("sales_tax_rate_not_found", "The record was not found.");
            }



            if (await db.SalesTaxRates.AnyAsync(x => x.Name == nameValue && x.Id != command.Id, cancellationToken).ConfigureAwait(false))
            {
                return Error.Conflict("sales_tax_rate_conflict", "A tax rate with the same values already exists.");
            }

            entity.Name = nameValue;
            entity.StateProvince = stateProvinceValue;
            entity.TaxType = command.TaxType;
            entity.TaxRate = command.TaxRate;

            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return await db.SalesTaxRates.AsNoTracking()
                .Where(x => x.Id == entity.Id)
                .Select(SalesTaxRateDto.Projection)
                .FirstAsync(cancellationToken)
                .ConfigureAwait(false);
        }
    }
}
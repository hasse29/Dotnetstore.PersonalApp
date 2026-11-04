using Dotnetstore.PersonalApp.Modules.Sales.Domain;
using Dotnetstore.PersonalApp.Modules.Sales.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Sales.Features.SalesTaxRates;

internal static class CreateSalesTaxRate
{
    public sealed record Command(string Name, string StateProvince, string TaxType, decimal TaxRate);

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




            if (await db.SalesTaxRates.AnyAsync(x => x.Name == nameValue, cancellationToken).ConfigureAwait(false))
            {
                return Error.Conflict("sales_tax_rate_conflict", "A tax rate with the same values already exists.");
            }

            var entity = new SalesTaxRate
            {
                Id = Guid.CreateVersion7(),
                Name = nameValue,
                StateProvince = stateProvinceValue,
                TaxType = command.TaxType,
                TaxRate = command.TaxRate,
            };

            db.SalesTaxRates.Add(entity);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return await db.SalesTaxRates.AsNoTracking()
                .Where(x => x.Id == entity.Id)
                .Select(SalesTaxRateDto.Projection)
                .FirstAsync(cancellationToken)
                .ConfigureAwait(false);
        }
    }
}
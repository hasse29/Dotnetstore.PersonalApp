using Dotnetstore.PersonalApp.Modules.Sales.Domain;
using Dotnetstore.PersonalApp.Modules.Sales.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Sales.Features.SalesTerritories;

internal static class CreateSalesTerritory
{
    public sealed record Command(string Name, string CountryRegionCode, string RegionGroup, decimal SalesYtd, decimal SalesLastYear, decimal CostYtd, decimal CostLastYear);

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(c => c.Name).NotEmpty().WithErrorCode("sales_territory_name_required").WithMessage("Name is required.").MaximumLength(50).WithErrorCode("sales_territory_name_too_long").WithMessage("Name is too long.");
            RuleFor(c => c.CountryRegionCode).NotEmpty().WithErrorCode("sales_territory_country_region_code_required").WithMessage("Country or region code is required.").MaximumLength(10).WithErrorCode("sales_territory_country_region_code_too_long").WithMessage("Country or region code is too long.");
            RuleFor(c => c.RegionGroup).NotEmpty().WithErrorCode("sales_territory_region_group_required").WithMessage("Group is required.").MaximumLength(50).WithErrorCode("sales_territory_region_group_too_long").WithMessage("Group is too long.");
            RuleFor(c => c.SalesYtd).InclusiveBetween(0m, 1000000000m).WithErrorCode("sales_territory_sales_ytd_out_of_range").WithMessage("Sales year to date is out of range.");
            RuleFor(c => c.SalesLastYear).InclusiveBetween(0m, 1000000000m).WithErrorCode("sales_territory_sales_last_year_out_of_range").WithMessage("Sales last year is out of range.");
            RuleFor(c => c.CostYtd).InclusiveBetween(0m, 1000000000m).WithErrorCode("sales_territory_cost_ytd_out_of_range").WithMessage("Cost year to date is out of range.");
            RuleFor(c => c.CostLastYear).InclusiveBetween(0m, 1000000000m).WithErrorCode("sales_territory_cost_last_year_out_of_range").WithMessage("Cost last year is out of range.");
        }
    }

    public sealed class Handler(IDbContextFactory<SalesDbContext> dbFactory, IValidator<Command> validator)
    {
        public async Task<Result<SalesTerritoryDto>> HandleAsync(Command command, CancellationToken cancellationToken)
        {
            var validation = await validator.ValidateAsync(command, cancellationToken).ConfigureAwait(false);
            if (!validation.IsValid)
            {
                return validation.ToError();
            }

            var nameValue = command.Name.Trim();
            var countryRegionCodeValue = command.CountryRegionCode.Trim();
            var regionGroupValue = command.RegionGroup.Trim();

            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);




            if (await db.SalesTerritories.AnyAsync(x => x.Name == nameValue, cancellationToken).ConfigureAwait(false))
            {
                return Error.Conflict("sales_territory_conflict", "A sales territory with the same values already exists.");
            }

            var entity = new SalesTerritory
            {
                Id = Guid.CreateVersion7(),
                Name = nameValue,
                CountryRegionCode = countryRegionCodeValue,
                RegionGroup = regionGroupValue,
                SalesYtd = command.SalesYtd,
                SalesLastYear = command.SalesLastYear,
                CostYtd = command.CostYtd,
                CostLastYear = command.CostLastYear,
            };

            db.SalesTerritories.Add(entity);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return await db.SalesTerritories.AsNoTracking()
                .Where(x => x.Id == entity.Id)
                .Select(SalesTerritoryDto.Projection)
                .FirstAsync(cancellationToken)
                .ConfigureAwait(false);
        }
    }
}
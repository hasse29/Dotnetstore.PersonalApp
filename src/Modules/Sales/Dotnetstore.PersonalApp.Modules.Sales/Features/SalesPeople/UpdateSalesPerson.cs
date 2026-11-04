using Dotnetstore.PersonalApp.Modules.Sales.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Sales.Features.SalesPeople;

internal static class UpdateSalesPerson
{
    public sealed record Command(Guid Id, string Name, Guid? UserId, Guid? TerritoryId, decimal? SalesQuota, decimal Bonus, decimal CommissionPct, decimal SalesYtd, decimal SalesLastYear);

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(c => c.Name).NotEmpty().WithErrorCode("sales_person_name_required").WithMessage("Name is required.").MaximumLength(100).WithErrorCode("sales_person_name_too_long").WithMessage("Name is too long.");
            RuleFor(c => c.SalesQuota).InclusiveBetween(0m, 1000000000m).WithErrorCode("sales_person_sales_quota_out_of_range").WithMessage("Sales quota is out of range.").When(c => c.SalesQuota is not null);
            RuleFor(c => c.Bonus).InclusiveBetween(0m, 1000000000m).WithErrorCode("sales_person_bonus_out_of_range").WithMessage("Bonus is out of range.");
            RuleFor(c => c.CommissionPct).InclusiveBetween(0m, 1m).WithErrorCode("sales_person_commission_pct_out_of_range").WithMessage("Commission (0-1) is out of range.");
            RuleFor(c => c.SalesYtd).InclusiveBetween(0m, 1000000000m).WithErrorCode("sales_person_sales_ytd_out_of_range").WithMessage("Sales year to date is out of range.");
            RuleFor(c => c.SalesLastYear).InclusiveBetween(0m, 1000000000m).WithErrorCode("sales_person_sales_last_year_out_of_range").WithMessage("Sales last year is out of range.");
        }
    }

    public sealed class Handler(IDbContextFactory<SalesDbContext> dbFactory, IValidator<Command> validator, ExternalReferences references)
    {
        public async Task<Result<SalesPersonDto>> HandleAsync(Command command, CancellationToken cancellationToken)
        {
            var validation = await validator.ValidateAsync(command, cancellationToken).ConfigureAwait(false);
            if (!validation.IsValid)
            {
                return validation.ToError();
            }

            var nameValue = command.Name.Trim();

            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            var entity = await db.SalesPeople.FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken).ConfigureAwait(false);
            if (entity is null)
            {
                return Error.NotFound("sales_person_not_found", "The record was not found.");
            }

            if (command.UserId is { } userId && !await references.UserExistsAsync(userId, cancellationToken).ConfigureAwait(false))
            {
                return Error.NotFound("sales_person_user_not_found", "User not found.");
            }

            if (command.TerritoryId is { } territoryId && !await db.SalesTerritories.AnyAsync(x => x.Id == territoryId, cancellationToken).ConfigureAwait(false))
            {
                return Error.NotFound("sales_person_territory_not_found", "Territory not found.");
            }

            if (await db.SalesPeople.AnyAsync(x => x.Name == nameValue && x.Id != command.Id, cancellationToken).ConfigureAwait(false))
            {
                return Error.Conflict("sales_person_conflict", "A salesperson with the same values already exists.");
            }

            entity.Name = nameValue;
            entity.UserId = command.UserId;
            entity.TerritoryId = command.TerritoryId;
            entity.SalesQuota = command.SalesQuota;
            entity.Bonus = command.Bonus;
            entity.CommissionPct = command.CommissionPct;
            entity.SalesYtd = command.SalesYtd;
            entity.SalesLastYear = command.SalesLastYear;

            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            var dto = await db.SalesPeople.AsNoTracking()
                .Where(x => x.Id == entity.Id)
                .Select(SalesPersonDto.Projection)
                .FirstAsync(cancellationToken)
                .ConfigureAwait(false);

            return dto.WithNames(await references.UserNamesAsync(cancellationToken).ConfigureAwait(false));
        }
    }
}
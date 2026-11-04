using Dotnetstore.PersonalApp.Modules.Sales.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Sales.Features.SalesTerritoryHistories;

internal static class UpdateSalesTerritoryHistory
{
    public sealed record Command(Guid Id, Guid SalesPersonId, Guid TerritoryId, DateOnly StartDate, DateOnly? EndDate);

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(c => c.SalesPersonId).NotEqual(Guid.Empty).WithErrorCode("sales_territory_history_sales_person_required").WithMessage("Salesperson is required.");
            RuleFor(c => c.TerritoryId).NotEqual(Guid.Empty).WithErrorCode("sales_territory_history_territory_required").WithMessage("Territory is required.");
            RuleFor(c => c).Must(c => c.EndDate is null || c.EndDate >= c.StartDate).WithErrorCode("sales_territory_history_end_before_start").WithMessage("The end date is before the start date.");
        }
    }

    public sealed class Handler(IDbContextFactory<SalesDbContext> dbFactory, IValidator<Command> validator)
    {
        public async Task<Result<SalesTerritoryHistoryDto>> HandleAsync(Command command, CancellationToken cancellationToken)
        {
            var validation = await validator.ValidateAsync(command, cancellationToken).ConfigureAwait(false);
            if (!validation.IsValid)
            {
                return validation.ToError();
            }



            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            var entity = await db.SalesTerritoryHistories.FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken).ConfigureAwait(false);
            if (entity is null)
            {
                return Error.NotFound("sales_territory_history_not_found", "The record was not found.");
            }

            if (!await db.SalesPeople.AnyAsync(x => x.Id == command.SalesPersonId, cancellationToken).ConfigureAwait(false))
            {
                return Error.NotFound("sales_territory_history_sales_person_not_found", "Salesperson not found.");
            }

            if (!await db.SalesTerritories.AnyAsync(x => x.Id == command.TerritoryId, cancellationToken).ConfigureAwait(false))
            {
                return Error.NotFound("sales_territory_history_territory_not_found", "Territory not found.");
            }



            entity.SalesPersonId = command.SalesPersonId;
            entity.TerritoryId = command.TerritoryId;
            entity.StartDate = command.StartDate;
            entity.EndDate = command.EndDate;

            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return await db.SalesTerritoryHistories.AsNoTracking()
                .Where(x => x.Id == entity.Id)
                .Select(SalesTerritoryHistoryDto.Projection)
                .FirstAsync(cancellationToken)
                .ConfigureAwait(false);
        }
    }
}
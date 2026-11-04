using Dotnetstore.PersonalApp.Modules.Sales.Domain;
using Dotnetstore.PersonalApp.Modules.Sales.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Sales.Features.SalesPersonQuotaHistories;

internal static class CreateSalesPersonQuotaHistory
{
    public sealed record Command(Guid SalesPersonId, DateOnly QuotaDate, decimal SalesQuota);

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(c => c.SalesPersonId).NotEqual(Guid.Empty).WithErrorCode("sales_person_quota_history_sales_person_required").WithMessage("Salesperson is required.");
            RuleFor(c => c.SalesQuota).InclusiveBetween(0m, 1000000000m).WithErrorCode("sales_person_quota_history_sales_quota_out_of_range").WithMessage("Sales quota is out of range.");
        }
    }

    public sealed class Handler(IDbContextFactory<SalesDbContext> dbFactory, IValidator<Command> validator)
    {
        public async Task<Result<SalesPersonQuotaHistoryDto>> HandleAsync(Command command, CancellationToken cancellationToken)
        {
            var validation = await validator.ValidateAsync(command, cancellationToken).ConfigureAwait(false);
            if (!validation.IsValid)
            {
                return validation.ToError();
            }



            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);


            if (!await db.SalesPeople.AnyAsync(x => x.Id == command.SalesPersonId, cancellationToken).ConfigureAwait(false))
            {
                return Error.NotFound("sales_person_quota_history_sales_person_not_found", "Salesperson not found.");
            }

            if (await db.SalesPersonQuotaHistories.AnyAsync(x => x.SalesPersonId == command.SalesPersonId && x.QuotaDate == command.QuotaDate, cancellationToken).ConfigureAwait(false))
            {
                return Error.Conflict("sales_person_quota_history_conflict", "A quota history entry with the same values already exists.");
            }

            var entity = new SalesPersonQuotaHistory
            {
                Id = Guid.CreateVersion7(),
                SalesPersonId = command.SalesPersonId,
                QuotaDate = command.QuotaDate,
                SalesQuota = command.SalesQuota,
            };

            db.SalesPersonQuotaHistories.Add(entity);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return await db.SalesPersonQuotaHistories.AsNoTracking()
                .Where(x => x.Id == entity.Id)
                .Select(SalesPersonQuotaHistoryDto.Projection)
                .FirstAsync(cancellationToken)
                .ConfigureAwait(false);
        }
    }
}
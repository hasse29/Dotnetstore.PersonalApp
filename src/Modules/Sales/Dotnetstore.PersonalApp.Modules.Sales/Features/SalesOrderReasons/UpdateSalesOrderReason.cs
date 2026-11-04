using Dotnetstore.PersonalApp.Modules.Sales.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Sales.Features.SalesOrderReasons;

internal static class UpdateSalesOrderReason
{
    public sealed record Command(Guid Id, Guid SalesOrderId, Guid SalesReasonId);

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(c => c.SalesOrderId).NotEqual(Guid.Empty).WithErrorCode("sales_order_reason_sales_order_required").WithMessage("Sales order is required.");
            RuleFor(c => c.SalesReasonId).NotEqual(Guid.Empty).WithErrorCode("sales_order_reason_sales_reason_required").WithMessage("Sales reason is required.");
        }
    }

    public sealed class Handler(IDbContextFactory<SalesDbContext> dbFactory, IValidator<Command> validator)
    {
        public async Task<Result<SalesOrderReasonDto>> HandleAsync(Command command, CancellationToken cancellationToken)
        {
            var validation = await validator.ValidateAsync(command, cancellationToken).ConfigureAwait(false);
            if (!validation.IsValid)
            {
                return validation.ToError();
            }



            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            var entity = await db.SalesOrderReasons.FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken).ConfigureAwait(false);
            if (entity is null)
            {
                return Error.NotFound("sales_order_reason_not_found", "The record was not found.");
            }

            if (!await db.SalesOrders.AnyAsync(x => x.Id == command.SalesOrderId, cancellationToken).ConfigureAwait(false))
            {
                return Error.NotFound("sales_order_reason_sales_order_not_found", "Sales order not found.");
            }

            if (!await db.SalesReasons.AnyAsync(x => x.Id == command.SalesReasonId, cancellationToken).ConfigureAwait(false))
            {
                return Error.NotFound("sales_order_reason_sales_reason_not_found", "Sales reason not found.");
            }

            if (await db.SalesOrderReasons.AnyAsync(x => x.SalesOrderId == command.SalesOrderId && x.SalesReasonId == command.SalesReasonId && x.Id != command.Id, cancellationToken).ConfigureAwait(false))
            {
                return Error.Conflict("sales_order_reason_conflict", "A order reason with the same values already exists.");
            }

            entity.SalesOrderId = command.SalesOrderId;
            entity.SalesReasonId = command.SalesReasonId;

            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return await db.SalesOrderReasons.AsNoTracking()
                .Where(x => x.Id == entity.Id)
                .Select(SalesOrderReasonDto.Projection)
                .FirstAsync(cancellationToken)
                .ConfigureAwait(false);
        }
    }
}
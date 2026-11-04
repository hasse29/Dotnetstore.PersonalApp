using Dotnetstore.PersonalApp.Modules.Sales.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Sales.Features.SalesOrders;

internal static class UpdateSalesOrder
{
    public sealed record Command(Guid Id, string Number, string Status, bool OnlineOrderFlag, DateOnly OrderDate, DateOnly DueDate, DateOnly? ShipDate, string? PurchaseOrderNumber, Guid CustomerId, Guid? SalesPersonId, Guid? TerritoryId, Guid? ShipMethodId, decimal TaxAmt, decimal Freight, string? Comment);

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(c => c.Number).NotEmpty().WithErrorCode("sales_order_number_required").WithMessage("Order number is required.").MaximumLength(25).WithErrorCode("sales_order_number_too_long").WithMessage("Order number is too long.");
            RuleFor(c => c.Status).Must(v => v is "I" or "A" or "B" or "R" or "S" or "C").WithErrorCode("sales_order_status_invalid").WithMessage("Status is not valid.");
            RuleFor(c => c.PurchaseOrderNumber).MaximumLength(25).WithErrorCode("sales_order_purchase_order_number_too_long").WithMessage("Customer's purchase order is too long.");
            RuleFor(c => c.CustomerId).NotEqual(Guid.Empty).WithErrorCode("sales_order_customer_required").WithMessage("Customer is required.");
            RuleFor(c => c.TaxAmt).InclusiveBetween(0m, 1000000000m).WithErrorCode("sales_order_tax_amt_out_of_range").WithMessage("Tax is out of range.");
            RuleFor(c => c.Freight).InclusiveBetween(0m, 1000000000m).WithErrorCode("sales_order_freight_out_of_range").WithMessage("Freight is out of range.");
            RuleFor(c => c.Comment).MaximumLength(128).WithErrorCode("sales_order_comment_too_long").WithMessage("Comment is too long.");
            RuleFor(c => c).Must(c => c.ShipDate is null || c.ShipDate >= c.OrderDate).WithErrorCode("sales_order_end_before_start").WithMessage("The end date is before the start date.");
        }
    }

    public sealed class Handler(IDbContextFactory<SalesDbContext> dbFactory, IValidator<Command> validator, ExternalReferences references)
    {
        public async Task<Result<SalesOrderDto>> HandleAsync(Command command, CancellationToken cancellationToken)
        {
            var validation = await validator.ValidateAsync(command, cancellationToken).ConfigureAwait(false);
            if (!validation.IsValid)
            {
                return validation.ToError();
            }

            var numberValue = command.Number.Trim();
            var purchaseOrderNumberValue = SalesRules.NormalizeOptional(command.PurchaseOrderNumber);
            var commentValue = SalesRules.NormalizeOptional(command.Comment);

            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            var entity = await db.SalesOrders.FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken).ConfigureAwait(false);
            if (entity is null)
            {
                return Error.NotFound("sales_order_not_found", "The record was not found.");
            }

            if (!await db.Customers.AnyAsync(x => x.Id == command.CustomerId, cancellationToken).ConfigureAwait(false))
            {
                return Error.NotFound("sales_order_customer_not_found", "Customer not found.");
            }

            if (command.SalesPersonId is { } salesPersonId && !await db.SalesPeople.AnyAsync(x => x.Id == salesPersonId, cancellationToken).ConfigureAwait(false))
            {
                return Error.NotFound("sales_order_sales_person_not_found", "Salesperson not found.");
            }

            if (command.TerritoryId is { } territoryId && !await db.SalesTerritories.AnyAsync(x => x.Id == territoryId, cancellationToken).ConfigureAwait(false))
            {
                return Error.NotFound("sales_order_territory_not_found", "Territory not found.");
            }

            if (command.ShipMethodId is { } shipMethodId && !await references.ShipMethodExistsAsync(shipMethodId, cancellationToken).ConfigureAwait(false))
            {
                return Error.NotFound("sales_order_ship_method_not_found", "Ship method not found.");
            }

            if (await db.SalesOrders.AnyAsync(x => x.Number == numberValue && x.Id != command.Id, cancellationToken).ConfigureAwait(false))
            {
                return Error.Conflict("sales_order_conflict", "A sales order with the same values already exists.");
            }

            entity.Number = numberValue;
            entity.Status = command.Status;
            entity.OnlineOrderFlag = command.OnlineOrderFlag;
            entity.OrderDate = command.OrderDate;
            entity.DueDate = command.DueDate;
            entity.ShipDate = command.ShipDate;
            entity.PurchaseOrderNumber = purchaseOrderNumberValue;
            entity.CustomerId = command.CustomerId;
            entity.SalesPersonId = command.SalesPersonId;
            entity.TerritoryId = command.TerritoryId;
            entity.ShipMethodId = command.ShipMethodId;
            entity.TaxAmt = command.TaxAmt;
            entity.Freight = command.Freight;
            entity.Comment = commentValue;

            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            await OrderTotals.RecalculateSalesOrderAsync(db, entity.Id, cancellationToken).ConfigureAwait(false);

            var dto = await db.SalesOrders.AsNoTracking()
                .Where(x => x.Id == entity.Id)
                .Select(SalesOrderDto.Projection)
                .FirstAsync(cancellationToken)
                .ConfigureAwait(false);

            return dto.WithNames(await references.ShipMethodNamesAsync(cancellationToken).ConfigureAwait(false));
        }
    }
}
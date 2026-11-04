using Dotnetstore.PersonalApp.Modules.Sales.Domain;
using Dotnetstore.PersonalApp.Modules.Sales.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Sales.Features.SalesOrders;

internal static class CreateSalesOrder
{
    public sealed record Command(string? Number, string Status, bool OnlineOrderFlag, DateOnly OrderDate, DateOnly DueDate, DateOnly? ShipDate, string? PurchaseOrderNumber, Guid CustomerId, Guid? SalesPersonId, Guid? TerritoryId, Guid? ShipMethodId, decimal TaxAmt, decimal Freight, string? Comment);

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(c => c.Number).MaximumLength(25).WithErrorCode("sales_order_number_too_long").WithMessage("Order number is too long.");
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

            var numberValue = SalesRules.NormalizeOptional(command.Number);
            var purchaseOrderNumberValue = SalesRules.NormalizeOptional(command.PurchaseOrderNumber);
            var commentValue = SalesRules.NormalizeOptional(command.Comment);

            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            numberValue ??= await NextNumberAsync(db, cancellationToken).ConfigureAwait(false);

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

            if (await db.SalesOrders.AnyAsync(x => x.Number == numberValue, cancellationToken).ConfigureAwait(false))
            {
                return Error.Conflict("sales_order_conflict", "A sales order with the same values already exists.");
            }

            var entity = new SalesOrder
            {
                Id = Guid.CreateVersion7(),
                Number = numberValue,
                Status = command.Status,
                OnlineOrderFlag = command.OnlineOrderFlag,
                OrderDate = command.OrderDate,
                DueDate = command.DueDate,
                ShipDate = command.ShipDate,
                PurchaseOrderNumber = purchaseOrderNumberValue,
                CustomerId = command.CustomerId,
                SalesPersonId = command.SalesPersonId,
                TerritoryId = command.TerritoryId,
                ShipMethodId = command.ShipMethodId,
                TaxAmt = command.TaxAmt,
                Freight = command.Freight,
                Comment = commentValue,
            };

            db.SalesOrders.Add(entity);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            await OrderTotals.RecalculateSalesOrderAsync(db, entity.Id, cancellationToken).ConfigureAwait(false);

            var dto = await db.SalesOrders.AsNoTracking()
                .Where(x => x.Id == entity.Id)
                .Select(SalesOrderDto.Projection)
                .FirstAsync(cancellationToken)
                .ConfigureAwait(false);

            return dto.WithNames(await references.ShipMethodNamesAsync(cancellationToken).ConfigureAwait(false));
        }

        private static async Task<string> NextNumberAsync(SalesDbContext db, CancellationToken cancellationToken)
        {
            var count = await db.SalesOrders.CountAsync(cancellationToken).ConfigureAwait(false);
            string candidate;
            do
            {
                count++;
                candidate = $"SO-{count:D6}";
            }
            while (await db.SalesOrders.AnyAsync(x => x.Number == candidate, cancellationToken).ConfigureAwait(false));

            return candidate;
        }
    }
}
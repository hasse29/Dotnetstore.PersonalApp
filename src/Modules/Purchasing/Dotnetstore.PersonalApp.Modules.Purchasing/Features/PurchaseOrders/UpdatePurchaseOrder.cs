using Dotnetstore.PersonalApp.Modules.Purchasing.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Purchasing.Features.PurchaseOrders;

internal static class UpdatePurchaseOrder
{
    public sealed record Command(Guid Id, string Number, string Status, Guid? BuyerId, Guid VendorId, Guid ShipMethodId, DateOnly OrderDate, DateOnly? ShipDate, decimal TaxAmt, decimal Freight);

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(c => c.Number).NotEmpty().WithErrorCode("purchase_order_number_required").WithMessage("Order number is required.").MaximumLength(25).WithErrorCode("purchase_order_number_too_long").WithMessage("Order number is too long.");
            RuleFor(c => c.Status).Must(v => v is "P" or "A" or "R" or "C").WithErrorCode("purchase_order_status_invalid").WithMessage("Status is not valid.");
            RuleFor(c => c.VendorId).NotEqual(Guid.Empty).WithErrorCode("purchase_order_vendor_required").WithMessage("Vendor is required.");
            RuleFor(c => c.ShipMethodId).NotEqual(Guid.Empty).WithErrorCode("purchase_order_ship_method_required").WithMessage("Ship method is required.");
            RuleFor(c => c.TaxAmt).InclusiveBetween(0m, 1000000000m).WithErrorCode("purchase_order_tax_amt_out_of_range").WithMessage("Tax is out of range.");
            RuleFor(c => c.Freight).InclusiveBetween(0m, 1000000000m).WithErrorCode("purchase_order_freight_out_of_range").WithMessage("Freight is out of range.");
            RuleFor(c => c).Must(c => c.ShipDate is null || c.ShipDate >= c.OrderDate).WithErrorCode("purchase_order_end_before_start").WithMessage("The end date is before the start date.");
        }
    }

    public sealed class Handler(IDbContextFactory<PurchasingDbContext> dbFactory, IValidator<Command> validator, ExternalReferences references)
    {
        public async Task<Result<PurchaseOrderDto>> HandleAsync(Command command, CancellationToken cancellationToken)
        {
            var validation = await validator.ValidateAsync(command, cancellationToken).ConfigureAwait(false);
            if (!validation.IsValid)
            {
                return validation.ToError();
            }

            var numberValue = command.Number.Trim();

            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            var entity = await db.PurchaseOrders.FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken).ConfigureAwait(false);
            if (entity is null)
            {
                return Error.NotFound("purchase_order_not_found", "The record was not found.");
            }

            if (command.BuyerId is { } buyerId && !await references.UserExistsAsync(buyerId, cancellationToken).ConfigureAwait(false))
            {
                return Error.NotFound("purchase_order_buyer_not_found", "Buyer not found.");
            }

            if (!await db.Vendors.AnyAsync(x => x.Id == command.VendorId, cancellationToken).ConfigureAwait(false))
            {
                return Error.NotFound("purchase_order_vendor_not_found", "Vendor not found.");
            }

            if (!await db.ShipMethods.AnyAsync(x => x.Id == command.ShipMethodId, cancellationToken).ConfigureAwait(false))
            {
                return Error.NotFound("purchase_order_ship_method_not_found", "Ship method not found.");
            }

            if (await db.PurchaseOrders.AnyAsync(x => x.Number == numberValue && x.Id != command.Id, cancellationToken).ConfigureAwait(false))
            {
                return Error.Conflict("purchase_order_conflict", "A purchase order with the same values already exists.");
            }

            entity.Number = numberValue;
            entity.Status = command.Status;
            entity.BuyerId = command.BuyerId;
            entity.VendorId = command.VendorId;
            entity.ShipMethodId = command.ShipMethodId;
            entity.OrderDate = command.OrderDate;
            entity.ShipDate = command.ShipDate;
            entity.TaxAmt = command.TaxAmt;
            entity.Freight = command.Freight;

            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            await OrderTotals.RecalculatePurchaseOrderAsync(db, entity.Id, cancellationToken).ConfigureAwait(false);

            var dto = await db.PurchaseOrders.AsNoTracking()
                .Where(x => x.Id == entity.Id)
                .Select(PurchaseOrderDto.Projection)
                .FirstAsync(cancellationToken)
                .ConfigureAwait(false);

            return dto.WithNames(await references.UserNamesAsync(cancellationToken).ConfigureAwait(false));
        }
    }
}
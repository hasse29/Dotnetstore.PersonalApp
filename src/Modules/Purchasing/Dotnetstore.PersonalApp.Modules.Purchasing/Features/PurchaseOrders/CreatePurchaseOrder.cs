using Dotnetstore.PersonalApp.Modules.Purchasing.Domain;
using Dotnetstore.PersonalApp.Modules.Purchasing.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Purchasing.Features.PurchaseOrders;

internal static class CreatePurchaseOrder
{
    public sealed record Command(string? Number, string Status, Guid? BuyerId, Guid VendorId, Guid ShipMethodId, DateOnly OrderDate, DateOnly? ShipDate, decimal TaxAmt, decimal Freight);

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(c => c.Number).MaximumLength(25).WithErrorCode("purchase_order_number_too_long").WithMessage("Order number is too long.");
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

            var numberValue = PurchasingRules.NormalizeOptional(command.Number);

            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            numberValue ??= await NextNumberAsync(db, cancellationToken).ConfigureAwait(false);

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

            if (await db.PurchaseOrders.AnyAsync(x => x.Number == numberValue, cancellationToken).ConfigureAwait(false))
            {
                return Error.Conflict("purchase_order_conflict", "A purchase order with the same values already exists.");
            }

            var entity = new PurchaseOrder
            {
                Id = Guid.CreateVersion7(),
                Number = numberValue,
                Status = command.Status,
                BuyerId = command.BuyerId,
                VendorId = command.VendorId,
                ShipMethodId = command.ShipMethodId,
                OrderDate = command.OrderDate,
                ShipDate = command.ShipDate,
                TaxAmt = command.TaxAmt,
                Freight = command.Freight,
            };

            db.PurchaseOrders.Add(entity);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            await OrderTotals.RecalculatePurchaseOrderAsync(db, entity.Id, cancellationToken).ConfigureAwait(false);

            var dto = await db.PurchaseOrders.AsNoTracking()
                .Where(x => x.Id == entity.Id)
                .Select(PurchaseOrderDto.Projection)
                .FirstAsync(cancellationToken)
                .ConfigureAwait(false);

            return dto.WithNames(await references.UserNamesAsync(cancellationToken).ConfigureAwait(false));
        }

        private static async Task<string> NextNumberAsync(PurchasingDbContext db, CancellationToken cancellationToken)
        {
            var count = await db.PurchaseOrders.CountAsync(cancellationToken).ConfigureAwait(false);
            string candidate;
            do
            {
                count++;
                candidate = $"PO-{count:D6}";
            }
            while (await db.PurchaseOrders.AnyAsync(x => x.Number == candidate, cancellationToken).ConfigureAwait(false));

            return candidate;
        }
    }
}
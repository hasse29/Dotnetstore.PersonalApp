using Dotnetstore.PersonalApp.Modules.Sales.Domain;
using Dotnetstore.PersonalApp.Modules.Sales.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Sales.Features.SalesOrderDetails;

internal static class CreateSalesOrderDetail
{
    public sealed record Command(Guid SalesOrderId, Guid ProductId, string? CarrierTrackingNumber, int OrderQty, Guid? SpecialOfferId, decimal UnitPrice, decimal UnitPriceDiscount);

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(c => c.SalesOrderId).NotEqual(Guid.Empty).WithErrorCode("sales_order_detail_sales_order_required").WithMessage("Sales order is required.");
            RuleFor(c => c.ProductId).NotEqual(Guid.Empty).WithErrorCode("sales_order_detail_product_required").WithMessage("Product is required.");
            RuleFor(c => c.CarrierTrackingNumber).MaximumLength(25).WithErrorCode("sales_order_detail_carrier_tracking_number_too_long").WithMessage("Tracking number is too long.");
            RuleFor(c => c.OrderQty).InclusiveBetween(1, 1000000).WithErrorCode("sales_order_detail_order_qty_out_of_range").WithMessage("Order quantity is out of range.");
            RuleFor(c => c.UnitPrice).InclusiveBetween(0m, 1000000000m).WithErrorCode("sales_order_detail_unit_price_out_of_range").WithMessage("Unit price is out of range.");
            RuleFor(c => c.UnitPriceDiscount).InclusiveBetween(0m, 1m).WithErrorCode("sales_order_detail_unit_price_discount_out_of_range").WithMessage("Discount (0-1) is out of range.");
        }
    }

    public sealed class Handler(IDbContextFactory<SalesDbContext> dbFactory, IValidator<Command> validator, ExternalReferences references)
    {
        public async Task<Result<SalesOrderDetailDto>> HandleAsync(Command command, CancellationToken cancellationToken)
        {
            var validation = await validator.ValidateAsync(command, cancellationToken).ConfigureAwait(false);
            if (!validation.IsValid)
            {
                return validation.ToError();
            }

            var carrierTrackingNumberValue = SalesRules.NormalizeOptional(command.CarrierTrackingNumber);

            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);


            if (!await db.SalesOrders.AnyAsync(x => x.Id == command.SalesOrderId, cancellationToken).ConfigureAwait(false))
            {
                return Error.NotFound("sales_order_detail_sales_order_not_found", "Sales order not found.");
            }

            if (!await references.ProductExistsAsync(command.ProductId, cancellationToken).ConfigureAwait(false))
            {
                return Error.NotFound("sales_order_detail_product_not_found", "Product not found.");
            }

            if (command.SpecialOfferId is { } specialOfferId && !await db.SpecialOffers.AnyAsync(x => x.Id == specialOfferId, cancellationToken).ConfigureAwait(false))
            {
                return Error.NotFound("sales_order_detail_special_offer_not_found", "Special offer not found.");
            }



            var entity = new SalesOrderDetail
            {
                Id = Guid.CreateVersion7(),
                SalesOrderId = command.SalesOrderId,
                ProductId = command.ProductId,
                CarrierTrackingNumber = carrierTrackingNumberValue,
                OrderQty = command.OrderQty,
                SpecialOfferId = command.SpecialOfferId,
                UnitPrice = command.UnitPrice,
                UnitPriceDiscount = command.UnitPriceDiscount,
                LineTotal = command.UnitPrice * command.OrderQty * (1m - command.UnitPriceDiscount),
            };

            db.SalesOrderDetails.Add(entity);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            await OrderTotals.RecalculateSalesOrderAsync(db, entity.SalesOrderId, cancellationToken).ConfigureAwait(false);

            var dto = await db.SalesOrderDetails.AsNoTracking()
                .Where(x => x.Id == entity.Id)
                .Select(SalesOrderDetailDto.Projection)
                .FirstAsync(cancellationToken)
                .ConfigureAwait(false);

            return dto.WithNames(await references.ProductNamesAsync(cancellationToken).ConfigureAwait(false));
        }
    }
}
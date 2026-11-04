using Dotnetstore.PersonalApp.Modules.Purchasing.Domain;
using Dotnetstore.PersonalApp.Modules.Purchasing.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Purchasing.Features.PurchaseOrderDetails;

internal static class CreatePurchaseOrderDetail
{
    public sealed record Command(Guid PurchaseOrderId, Guid ProductId, DateOnly DueDate, int OrderQty, decimal UnitPrice, decimal ReceivedQty, decimal RejectedQty);

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(c => c.PurchaseOrderId).NotEqual(Guid.Empty).WithErrorCode("purchase_order_detail_purchase_order_required").WithMessage("Purchase order is required.");
            RuleFor(c => c.ProductId).NotEqual(Guid.Empty).WithErrorCode("purchase_order_detail_product_required").WithMessage("Product is required.");
            RuleFor(c => c.OrderQty).InclusiveBetween(1, 1000000).WithErrorCode("purchase_order_detail_order_qty_out_of_range").WithMessage("Order quantity is out of range.");
            RuleFor(c => c.UnitPrice).InclusiveBetween(0m, 1000000000m).WithErrorCode("purchase_order_detail_unit_price_out_of_range").WithMessage("Unit price is out of range.");
            RuleFor(c => c.ReceivedQty).InclusiveBetween(0m, 1000000000m).WithErrorCode("purchase_order_detail_received_qty_out_of_range").WithMessage("Received quantity is out of range.");
            RuleFor(c => c.RejectedQty).InclusiveBetween(0m, 1000000000m).WithErrorCode("purchase_order_detail_rejected_qty_out_of_range").WithMessage("Rejected quantity is out of range.");
        }
    }

    public sealed class Handler(IDbContextFactory<PurchasingDbContext> dbFactory, IValidator<Command> validator, ExternalReferences references)
    {
        public async Task<Result<PurchaseOrderDetailDto>> HandleAsync(Command command, CancellationToken cancellationToken)
        {
            var validation = await validator.ValidateAsync(command, cancellationToken).ConfigureAwait(false);
            if (!validation.IsValid)
            {
                return validation.ToError();
            }



            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);


            if (!await db.PurchaseOrders.AnyAsync(x => x.Id == command.PurchaseOrderId, cancellationToken).ConfigureAwait(false))
            {
                return Error.NotFound("purchase_order_detail_purchase_order_not_found", "Purchase order not found.");
            }

            if (!await references.ProductExistsAsync(command.ProductId, cancellationToken).ConfigureAwait(false))
            {
                return Error.NotFound("purchase_order_detail_product_not_found", "Product not found.");
            }



            var entity = new PurchaseOrderDetail
            {
                Id = Guid.CreateVersion7(),
                PurchaseOrderId = command.PurchaseOrderId,
                ProductId = command.ProductId,
                DueDate = command.DueDate,
                OrderQty = command.OrderQty,
                UnitPrice = command.UnitPrice,
                ReceivedQty = command.ReceivedQty,
                RejectedQty = command.RejectedQty,
                LineTotal = command.OrderQty * command.UnitPrice,
                StockedQty = command.ReceivedQty - command.RejectedQty,
            };

            db.PurchaseOrderDetails.Add(entity);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            await OrderTotals.RecalculatePurchaseOrderAsync(db, entity.PurchaseOrderId, cancellationToken).ConfigureAwait(false);

            var dto = await db.PurchaseOrderDetails.AsNoTracking()
                .Where(x => x.Id == entity.Id)
                .Select(PurchaseOrderDetailDto.Projection)
                .FirstAsync(cancellationToken)
                .ConfigureAwait(false);

            return dto.WithNames(await references.ProductNamesAsync(cancellationToken).ConfigureAwait(false));
        }
    }
}
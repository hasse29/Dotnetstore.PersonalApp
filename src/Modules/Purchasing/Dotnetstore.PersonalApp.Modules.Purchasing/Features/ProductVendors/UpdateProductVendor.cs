using Dotnetstore.PersonalApp.Modules.Purchasing.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Purchasing.Features.ProductVendors;

internal static class UpdateProductVendor
{
    public sealed record Command(Guid Id, Guid ProductId, Guid VendorId, int AverageLeadTime, decimal StandardPrice, decimal? LastReceiptCost, DateOnly? LastReceiptDate, int MinOrderQty, int MaxOrderQty, int? OnOrderQty, Guid UnitId);

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(c => c.ProductId).NotEqual(Guid.Empty).WithErrorCode("product_vendor_product_required").WithMessage("Product is required.");
            RuleFor(c => c.VendorId).NotEqual(Guid.Empty).WithErrorCode("product_vendor_vendor_required").WithMessage("Vendor is required.");
            RuleFor(c => c.AverageLeadTime).InclusiveBetween(0, 1000000).WithErrorCode("product_vendor_average_lead_time_out_of_range").WithMessage("Average lead time (days) is out of range.");
            RuleFor(c => c.StandardPrice).InclusiveBetween(0m, 1000000000m).WithErrorCode("product_vendor_standard_price_out_of_range").WithMessage("Standard price is out of range.");
            RuleFor(c => c.LastReceiptCost).InclusiveBetween(0m, 1000000000m).WithErrorCode("product_vendor_last_receipt_cost_out_of_range").WithMessage("Last receipt cost is out of range.").When(c => c.LastReceiptCost is not null);
            RuleFor(c => c.MinOrderQty).InclusiveBetween(0, 1000000).WithErrorCode("product_vendor_min_order_qty_out_of_range").WithMessage("Minimum order quantity is out of range.");
            RuleFor(c => c.MaxOrderQty).InclusiveBetween(0, 1000000).WithErrorCode("product_vendor_max_order_qty_out_of_range").WithMessage("Maximum order quantity is out of range.");
            RuleFor(c => c.OnOrderQty).InclusiveBetween(0, 1000000).WithErrorCode("product_vendor_on_order_qty_out_of_range").WithMessage("Quantity on order is out of range.").When(c => c.OnOrderQty is not null);
            RuleFor(c => c.UnitId).NotEqual(Guid.Empty).WithErrorCode("product_vendor_unit_required").WithMessage("Unit is required.");
        }
    }

    public sealed class Handler(IDbContextFactory<PurchasingDbContext> dbFactory, IValidator<Command> validator, ExternalReferences references)
    {
        public async Task<Result<ProductVendorDto>> HandleAsync(Command command, CancellationToken cancellationToken)
        {
            var validation = await validator.ValidateAsync(command, cancellationToken).ConfigureAwait(false);
            if (!validation.IsValid)
            {
                return validation.ToError();
            }



            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            var entity = await db.ProductVendors.FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken).ConfigureAwait(false);
            if (entity is null)
            {
                return Error.NotFound("product_vendor_not_found", "The record was not found.");
            }

            if (!await references.ProductExistsAsync(command.ProductId, cancellationToken).ConfigureAwait(false))
            {
                return Error.NotFound("product_vendor_product_not_found", "Product not found.");
            }

            if (!await db.Vendors.AnyAsync(x => x.Id == command.VendorId, cancellationToken).ConfigureAwait(false))
            {
                return Error.NotFound("product_vendor_vendor_not_found", "Vendor not found.");
            }

            if (!await references.UnitMeasureExistsAsync(command.UnitId, cancellationToken).ConfigureAwait(false))
            {
                return Error.NotFound("product_vendor_unit_not_found", "Unit not found.");
            }

            if (await db.ProductVendors.AnyAsync(x => x.ProductId == command.ProductId && x.VendorId == command.VendorId && x.Id != command.Id, cancellationToken).ConfigureAwait(false))
            {
                return Error.Conflict("product_vendor_conflict", "A vendor price with the same values already exists.");
            }

            entity.ProductId = command.ProductId;
            entity.VendorId = command.VendorId;
            entity.AverageLeadTime = command.AverageLeadTime;
            entity.StandardPrice = command.StandardPrice;
            entity.LastReceiptCost = command.LastReceiptCost;
            entity.LastReceiptDate = command.LastReceiptDate;
            entity.MinOrderQty = command.MinOrderQty;
            entity.MaxOrderQty = command.MaxOrderQty;
            entity.OnOrderQty = command.OnOrderQty;
            entity.UnitId = command.UnitId;

            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            var dto = await db.ProductVendors.AsNoTracking()
                .Where(x => x.Id == entity.Id)
                .Select(ProductVendorDto.Projection)
                .FirstAsync(cancellationToken)
                .ConfigureAwait(false);

            return dto.WithNames(await references.ProductNamesAsync(cancellationToken).ConfigureAwait(false), await references.UnitMeasureNamesAsync(cancellationToken).ConfigureAwait(false));
        }
    }
}
using Dotnetstore.PersonalApp.Modules.Production.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Production.Features.WorkOrders;

internal static class UpdateWorkOrder
{
    public sealed record Command(Guid Id, string Number, Guid ProductId, int OrderQty, int ScrappedQty, DateOnly StartDate, DateOnly? EndDate, DateOnly DueDate, Guid? ScrapReasonId);

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(c => c.Number).NotEmpty().WithErrorCode("work_order_number_required").WithMessage("Work order number is required.").MaximumLength(25).WithErrorCode("work_order_number_too_long").WithMessage("Work order number is too long.");
            RuleFor(c => c.ProductId).NotEqual(Guid.Empty).WithErrorCode("work_order_product_required").WithMessage("Product is required.");
            RuleFor(c => c.OrderQty).InclusiveBetween(1, 1000000).WithErrorCode("work_order_order_qty_out_of_range").WithMessage("Order quantity is out of range.");
            RuleFor(c => c.ScrappedQty).InclusiveBetween(0, 1000000).WithErrorCode("work_order_scrapped_qty_out_of_range").WithMessage("Scrapped quantity is out of range.");
            RuleFor(c => c).Must(c => c.EndDate is null || c.EndDate >= c.StartDate).WithErrorCode("work_order_end_before_start").WithMessage("The end date is before the start date.");
        }
    }

    public sealed class Handler(IDbContextFactory<ProductionDbContext> dbFactory, IValidator<Command> validator)
    {
        public async Task<Result<WorkOrderDto>> HandleAsync(Command command, CancellationToken cancellationToken)
        {
            var validation = await validator.ValidateAsync(command, cancellationToken).ConfigureAwait(false);
            if (!validation.IsValid)
            {
                return validation.ToError();
            }

            var numberValue = command.Number.Trim();

            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            var entity = await db.WorkOrders.FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken).ConfigureAwait(false);
            if (entity is null)
            {
                return Error.NotFound("work_order_not_found", "The record was not found.");
            }

            if (!await db.Products.AnyAsync(x => x.Id == command.ProductId, cancellationToken).ConfigureAwait(false))
            {
                return Error.NotFound("work_order_product_not_found", "Product not found.");
            }

            if (command.ScrapReasonId is { } scrapReasonId && !await db.ScrapReasons.AnyAsync(x => x.Id == scrapReasonId, cancellationToken).ConfigureAwait(false))
            {
                return Error.NotFound("work_order_scrap_reason_not_found", "Scrap reason not found.");
            }

            if (await db.WorkOrders.AnyAsync(x => x.Number == numberValue && x.Id != command.Id, cancellationToken).ConfigureAwait(false))
            {
                return Error.Conflict("work_order_conflict", "A work order with the same values already exists.");
            }

            entity.Number = numberValue;
            entity.ProductId = command.ProductId;
            entity.OrderQty = command.OrderQty;
            entity.ScrappedQty = command.ScrappedQty;
            entity.StartDate = command.StartDate;
            entity.EndDate = command.EndDate;
            entity.DueDate = command.DueDate;
            entity.ScrapReasonId = command.ScrapReasonId;

            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return await db.WorkOrders.AsNoTracking()
                .Where(x => x.Id == entity.Id)
                .Select(WorkOrderDto.Projection)
                .FirstAsync(cancellationToken)
                .ConfigureAwait(false);
        }
    }
}
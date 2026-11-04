using Dotnetstore.PersonalApp.Modules.Production.Domain;
using Dotnetstore.PersonalApp.Modules.Production.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Production.Features.WorkOrders;

internal static class CreateWorkOrder
{
    public sealed record Command(string? Number, Guid ProductId, int OrderQty, int ScrappedQty, DateOnly StartDate, DateOnly? EndDate, DateOnly DueDate, Guid? ScrapReasonId);

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(c => c.Number).MaximumLength(25).WithErrorCode("work_order_number_too_long").WithMessage("Work order number is too long.");
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

            var numberValue = ProductionRules.NormalizeOptional(command.Number);

            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            numberValue ??= await NextNumberAsync(db, cancellationToken).ConfigureAwait(false);

            if (!await db.Products.AnyAsync(x => x.Id == command.ProductId, cancellationToken).ConfigureAwait(false))
            {
                return Error.NotFound("work_order_product_not_found", "Product not found.");
            }

            if (command.ScrapReasonId is { } scrapReasonId && !await db.ScrapReasons.AnyAsync(x => x.Id == scrapReasonId, cancellationToken).ConfigureAwait(false))
            {
                return Error.NotFound("work_order_scrap_reason_not_found", "Scrap reason not found.");
            }

            if (await db.WorkOrders.AnyAsync(x => x.Number == numberValue, cancellationToken).ConfigureAwait(false))
            {
                return Error.Conflict("work_order_conflict", "A work order with the same values already exists.");
            }

            var entity = new WorkOrder
            {
                Id = Guid.CreateVersion7(),
                Number = numberValue,
                ProductId = command.ProductId,
                OrderQty = command.OrderQty,
                ScrappedQty = command.ScrappedQty,
                StartDate = command.StartDate,
                EndDate = command.EndDate,
                DueDate = command.DueDate,
                ScrapReasonId = command.ScrapReasonId,
            };

            db.WorkOrders.Add(entity);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return await db.WorkOrders.AsNoTracking()
                .Where(x => x.Id == entity.Id)
                .Select(WorkOrderDto.Projection)
                .FirstAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        private static async Task<string> NextNumberAsync(ProductionDbContext db, CancellationToken cancellationToken)
        {
            var count = await db.WorkOrders.CountAsync(cancellationToken).ConfigureAwait(false);
            string candidate;
            do
            {
                count++;
                candidate = $"WO-{count:D6}";
            }
            while (await db.WorkOrders.AnyAsync(x => x.Number == candidate, cancellationToken).ConfigureAwait(false));

            return candidate;
        }
    }
}
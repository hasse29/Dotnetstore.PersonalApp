using Dotnetstore.PersonalApp.Modules.Production.Domain;
using Dotnetstore.PersonalApp.Modules.Production.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Production.Features.WorkOrderRoutings;

internal static class CreateWorkOrderRouting
{
    public sealed record Command(Guid WorkOrderId, int OperationSequence, Guid LocationId, DateOnly ScheduledStartDate, DateOnly ScheduledEndDate, DateOnly? ActualStartDate, DateOnly? ActualEndDate, decimal? ActualResourceHrs, decimal PlannedCost, decimal? ActualCost);

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(c => c.WorkOrderId).NotEqual(Guid.Empty).WithErrorCode("work_order_routing_work_order_required").WithMessage("Work order is required.");
            RuleFor(c => c.OperationSequence).InclusiveBetween(1, 10000).WithErrorCode("work_order_routing_operation_sequence_out_of_range").WithMessage("Operation sequence is out of range.");
            RuleFor(c => c.LocationId).NotEqual(Guid.Empty).WithErrorCode("work_order_routing_location_required").WithMessage("Location is required.");
            RuleFor(c => c.ActualResourceHrs).InclusiveBetween(0m, 1000000000m).WithErrorCode("work_order_routing_actual_resource_hrs_out_of_range").WithMessage("Actual resource hours is out of range.").When(c => c.ActualResourceHrs is not null);
            RuleFor(c => c.PlannedCost).InclusiveBetween(0m, 1000000000m).WithErrorCode("work_order_routing_planned_cost_out_of_range").WithMessage("Planned cost is out of range.");
            RuleFor(c => c.ActualCost).InclusiveBetween(0m, 1000000000m).WithErrorCode("work_order_routing_actual_cost_out_of_range").WithMessage("Actual cost is out of range.").When(c => c.ActualCost is not null);
            RuleFor(c => c).Must(c => c.ScheduledEndDate >= c.ScheduledStartDate).WithErrorCode("work_order_routing_end_before_start").WithMessage("The end date is before the start date.");
        }
    }

    public sealed class Handler(IDbContextFactory<ProductionDbContext> dbFactory, IValidator<Command> validator)
    {
        public async Task<Result<WorkOrderRoutingDto>> HandleAsync(Command command, CancellationToken cancellationToken)
        {
            var validation = await validator.ValidateAsync(command, cancellationToken).ConfigureAwait(false);
            if (!validation.IsValid)
            {
                return validation.ToError();
            }



            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);


            if (!await db.WorkOrders.AnyAsync(x => x.Id == command.WorkOrderId, cancellationToken).ConfigureAwait(false))
            {
                return Error.NotFound("work_order_routing_work_order_not_found", "Work order not found.");
            }

            if (!await db.Locations.AnyAsync(x => x.Id == command.LocationId, cancellationToken).ConfigureAwait(false))
            {
                return Error.NotFound("work_order_routing_location_not_found", "Location not found.");
            }

            if (await db.WorkOrderRoutings.AnyAsync(x => x.WorkOrderId == command.WorkOrderId && x.OperationSequence == command.OperationSequence, cancellationToken).ConfigureAwait(false))
            {
                return Error.Conflict("work_order_routing_conflict", "A work order routing step with the same values already exists.");
            }

            var entity = new WorkOrderRouting
            {
                Id = Guid.CreateVersion7(),
                WorkOrderId = command.WorkOrderId,
                OperationSequence = command.OperationSequence,
                LocationId = command.LocationId,
                ScheduledStartDate = command.ScheduledStartDate,
                ScheduledEndDate = command.ScheduledEndDate,
                ActualStartDate = command.ActualStartDate,
                ActualEndDate = command.ActualEndDate,
                ActualResourceHrs = command.ActualResourceHrs,
                PlannedCost = command.PlannedCost,
                ActualCost = command.ActualCost,
            };

            db.WorkOrderRoutings.Add(entity);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return await db.WorkOrderRoutings.AsNoTracking()
                .Where(x => x.Id == entity.Id)
                .Select(WorkOrderRoutingDto.Projection)
                .FirstAsync(cancellationToken)
                .ConfigureAwait(false);
        }
    }
}
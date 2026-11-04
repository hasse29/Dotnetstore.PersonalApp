using System.Linq.Expressions;
using Dotnetstore.PersonalApp.Modules.Production.Domain;

namespace Dotnetstore.PersonalApp.Modules.Production.Features.WorkOrderRoutings;

internal sealed record WorkOrderRoutingDto(Guid Id, Guid WorkOrderId, string WorkOrderName, int OperationSequence, Guid LocationId, string LocationName, DateOnly ScheduledStartDate, DateOnly ScheduledEndDate, DateOnly? ActualStartDate, DateOnly? ActualEndDate, decimal? ActualResourceHrs, decimal PlannedCost, decimal? ActualCost)
{
    public static Expression<Func<WorkOrderRouting, WorkOrderRoutingDto>> Projection { get; } =
        t => new WorkOrderRoutingDto(t.Id, t.WorkOrderId, t.WorkOrder.Number, t.OperationSequence, t.LocationId, t.Location.Name, t.ScheduledStartDate, t.ScheduledEndDate, t.ActualStartDate, t.ActualEndDate, t.ActualResourceHrs, t.PlannedCost, t.ActualCost);
}
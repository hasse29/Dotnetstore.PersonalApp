namespace Dotnetstore.PersonalApp.Modules.Production.Domain;

/// <summary>work order routing step.</summary>
internal sealed class WorkOrderRouting
{
    public Guid Id { get; set; }

    public Guid WorkOrderId { get; set; }

    public WorkOrder WorkOrder { get; set; } = null!;

    public int OperationSequence { get; set; }

    public Guid LocationId { get; set; }

    public Location Location { get; set; } = null!;

    public DateOnly ScheduledStartDate { get; set; }

    public DateOnly ScheduledEndDate { get; set; }

    public DateOnly? ActualStartDate { get; set; }

    public DateOnly? ActualEndDate { get; set; }

    public decimal? ActualResourceHrs { get; set; }

    public decimal PlannedCost { get; set; }

    public decimal? ActualCost { get; set; }
}
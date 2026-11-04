namespace Dotnetstore.PersonalApp.Modules.Production.Domain;

/// <summary>work order.</summary>
internal sealed class WorkOrder
{
    public Guid Id { get; set; }

    public string Number { get; set; } = string.Empty;

    public Guid ProductId { get; set; }

    public Product Product { get; set; } = null!;

    public int OrderQty { get; set; }

    public int ScrappedQty { get; set; }

    public DateOnly StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public DateOnly DueDate { get; set; }

    public Guid? ScrapReasonId { get; set; }

    public ScrapReason? ScrapReason { get; set; }
}
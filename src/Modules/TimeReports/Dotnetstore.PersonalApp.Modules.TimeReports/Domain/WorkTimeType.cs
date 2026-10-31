namespace Dotnetstore.PersonalApp.Modules.TimeReports.Domain;

/// <summary>A user-managed kind of work time, e.g. Billing or Delivery.</summary>
internal sealed class WorkTimeType
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}

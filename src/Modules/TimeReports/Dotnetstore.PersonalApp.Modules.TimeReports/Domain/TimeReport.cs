namespace Dotnetstore.PersonalApp.Modules.TimeReports.Domain;

/// <summary>
/// Time one user worked for one client on one day. <see cref="ClientId"/> and <see cref="UserId"/> refer to objects owned by
/// other modules; they are not foreign keys, and reports are kept if the client or user is later deleted.
/// </summary>
internal sealed class TimeReport
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public Guid ClientId { get; set; }

    public Guid WorkTimeTypeId { get; set; }

    public DateOnly Date { get; set; }

    public TimeOnly StartTime { get; set; }

    public TimeOnly EndTime { get; set; }

    /// <summary>Hours worked as reported by the user; it may deliberately differ from end minus start (breaks, etc.).</summary>
    public decimal TotalHours { get; set; }

    public string? Location { get; set; }

    public decimal DrivingKilometers { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
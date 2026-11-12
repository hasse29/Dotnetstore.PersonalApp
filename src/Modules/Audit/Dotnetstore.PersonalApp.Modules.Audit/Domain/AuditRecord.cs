namespace Dotnetstore.PersonalApp.Modules.Audit.Domain;

/// <summary>One audit log line. Append-only: nothing in the app updates or deletes these.</summary>
internal sealed class AuditRecord
{
    public Guid Id { get; set; }

    public DateTimeOffset Timestamp { get; set; }

    public Guid? UserId { get; set; }

    public string? Username { get; set; }

    public string Operation { get; set; } = string.Empty;

    public bool IsWrite { get; set; }

    public Guid? EntityId { get; set; }

    public bool Succeeded { get; set; }

    public string? ErrorCode { get; set; }

    /// <summary>Comma separated names of the fields the request filled in. Names only, never values.</summary>
    public string? Fields { get; set; }

    public double ElapsedMs { get; set; }
}

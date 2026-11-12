namespace Dotnetstore.PersonalApp.Shared.Audit;

public enum AuditKind
{
    Read = 0,
    Write = 1,
}

/// <summary>
/// One line of the audit log: who did what, to which entity, and how it went. Field names are recorded, never values, so the log
/// holds no customer or contact data.
/// </summary>
public sealed record AuditEntry(
    DateTimeOffset Timestamp,
    Guid? UserId,
    string? Username,
    string Operation,
    AuditKind Kind,
    Guid? EntityId,
    bool Succeeded,
    string? ErrorCode,
    IReadOnlyList<string> Fields,
    double ElapsedMs);

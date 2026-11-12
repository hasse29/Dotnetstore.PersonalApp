namespace Dotnetstore.PersonalApp.Modules.Audit.Features.Entries;

internal sealed record AuditEntryDto(
    Guid Id,
    DateTimeOffset Timestamp,
    string User,
    string Operation,
    bool IsWrite,
    Guid? EntityId,
    string? Fields,
    bool Succeeded,
    string? ErrorCode)
{
    public string When => Timestamp.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);

    public string Outcome => Succeeded ? "OK" : ErrorCode ?? "error";

    public string Entity => EntityId?.ToString() ?? string.Empty;
}

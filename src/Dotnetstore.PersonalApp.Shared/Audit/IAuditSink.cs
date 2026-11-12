namespace Dotnetstore.PersonalApp.Shared.Audit;

/// <summary>Where audit entries go. Implementations must not throw and must not block the caller.</summary>
public interface IAuditSink
{
    void Record(AuditEntry entry);
}

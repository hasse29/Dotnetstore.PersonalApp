using System.Resources;

namespace Dotnetstore.PersonalApp.Modules.Audit.Resources;

internal static class AuditStrings
{
    public static ResourceManager ResourceManager { get; } =
        new("Dotnetstore.PersonalApp.Modules.Audit.Resources.AuditStrings", typeof(AuditStrings).Assembly);
}

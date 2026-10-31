using System.Resources;

namespace Dotnetstore.PersonalApp.Host.Resources;

internal static class HostStrings
{
    public static ResourceManager ResourceManager { get; } =
        new("Dotnetstore.PersonalApp.Host.Resources.HostStrings", typeof(HostStrings).Assembly);
}

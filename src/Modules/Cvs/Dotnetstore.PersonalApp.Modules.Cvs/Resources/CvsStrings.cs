using System.Resources;

namespace Dotnetstore.PersonalApp.Modules.Cvs.Resources;

internal static class CvsStrings
{
    public static ResourceManager ResourceManager { get; } =
        new("Dotnetstore.PersonalApp.Modules.Cvs.Resources.CvsStrings", typeof(CvsStrings).Assembly);
}

using System.Resources;

namespace Dotnetstore.PersonalApp.Shared.Resources;

/// <summary>Strings every screen may use (e.g. Cancel); registered automatically by <c>Localizer</c>.</summary>
public static class SharedStrings
{
    public static ResourceManager ResourceManager { get; } =
        new("Dotnetstore.PersonalApp.Shared.Resources.SharedStrings", typeof(SharedStrings).Assembly);
}

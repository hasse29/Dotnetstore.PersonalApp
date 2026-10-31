using System.Resources;

namespace Dotnetstore.PersonalApp.Modules.Users.Resources;

internal static class UsersStrings
{
    public static ResourceManager ResourceManager { get; } =
        new("Dotnetstore.PersonalApp.Modules.Users.Resources.UsersStrings", typeof(UsersStrings).Assembly);
}

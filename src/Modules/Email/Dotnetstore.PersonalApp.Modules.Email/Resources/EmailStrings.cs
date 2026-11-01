using System.Resources;

namespace Dotnetstore.PersonalApp.Modules.Email.Resources;

internal static class EmailStrings
{
    public static ResourceManager ResourceManager { get; } =
        new("Dotnetstore.PersonalApp.Modules.Email.Resources.EmailStrings", typeof(EmailStrings).Assembly);
}

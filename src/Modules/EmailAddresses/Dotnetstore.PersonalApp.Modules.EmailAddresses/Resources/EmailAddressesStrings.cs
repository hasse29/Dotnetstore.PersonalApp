using System.Resources;

namespace Dotnetstore.PersonalApp.Modules.EmailAddresses.Resources;

internal static class EmailAddressesStrings
{
    public static ResourceManager ResourceManager { get; } =
        new("Dotnetstore.PersonalApp.Modules.EmailAddresses.Resources.EmailAddressesStrings", typeof(EmailAddressesStrings).Assembly);
}

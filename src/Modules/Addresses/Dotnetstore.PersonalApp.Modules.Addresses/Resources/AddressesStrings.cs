using System.Resources;

namespace Dotnetstore.PersonalApp.Modules.Addresses.Resources;

internal static class AddressesStrings
{
    public static ResourceManager ResourceManager { get; } =
        new("Dotnetstore.PersonalApp.Modules.Addresses.Resources.AddressesStrings", typeof(AddressesStrings).Assembly);
}

using System.Resources;

namespace Dotnetstore.PersonalApp.Modules.PhoneNumbers.Resources;

internal static class PhoneNumbersStrings
{
    public static ResourceManager ResourceManager { get; } =
        new("Dotnetstore.PersonalApp.Modules.PhoneNumbers.Resources.PhoneNumbersStrings", typeof(PhoneNumbersStrings).Assembly);
}

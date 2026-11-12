using System.Resources;

namespace Dotnetstore.PersonalApp.Modules.Contact.Resources;

internal static class ContactStrings
{
    public static ResourceManager ResourceManager { get; } =
        new("Dotnetstore.PersonalApp.Modules.Contact.Resources.ContactStrings", typeof(ContactStrings).Assembly);
}

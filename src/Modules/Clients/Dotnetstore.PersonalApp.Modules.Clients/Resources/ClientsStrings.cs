using System.Resources;

namespace Dotnetstore.PersonalApp.Modules.Clients.Resources;

internal static class ClientsStrings
{
    public static ResourceManager ResourceManager { get; } =
        new("Dotnetstore.PersonalApp.Modules.Clients.Resources.ClientsStrings", typeof(ClientsStrings).Assembly);
}

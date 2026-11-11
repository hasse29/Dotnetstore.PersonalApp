using System.Resources;

namespace Dotnetstore.PersonalApp.Modules.Sales.Resources;

internal static class SalesStrings
{
    public static ResourceManager ResourceManager { get; } =
        new("Dotnetstore.PersonalApp.Modules.Sales.Resources.SalesStrings", typeof(SalesStrings).Assembly);
}

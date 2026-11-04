using System.Resources;

namespace Dotnetstore.PersonalApp.Modules.Production.Resources;

internal static class ProductionStrings
{
    public static ResourceManager ResourceManager { get; } =
        new("Dotnetstore.PersonalApp.Modules.Production.Resources.ProductionStrings", typeof(ProductionStrings).Assembly);
}
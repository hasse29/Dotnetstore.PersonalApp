using System.Resources;

namespace Dotnetstore.PersonalApp.Modules.Products.Resources;

internal static class ProductsStrings
{
    public static ResourceManager ResourceManager { get; } =
        new("Dotnetstore.PersonalApp.Modules.Products.Resources.ProductsStrings", typeof(ProductsStrings).Assembly);
}
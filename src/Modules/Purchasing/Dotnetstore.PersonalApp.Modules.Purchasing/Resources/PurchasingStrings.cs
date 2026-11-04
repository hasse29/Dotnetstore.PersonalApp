using System.Resources;

namespace Dotnetstore.PersonalApp.Modules.Purchasing.Resources;

internal static class PurchasingStrings
{
    public static ResourceManager ResourceManager { get; } =
        new("Dotnetstore.PersonalApp.Modules.Purchasing.Resources.PurchasingStrings", typeof(PurchasingStrings).Assembly);
}
using System.Resources;

namespace Dotnetstore.PersonalApp.Modules.Inventory.Resources;

internal static class InventoryStrings
{
    public static ResourceManager ResourceManager { get; } =
        new("Dotnetstore.PersonalApp.Modules.Inventory.Resources.InventoryStrings", typeof(InventoryStrings).Assembly);
}
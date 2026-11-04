using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Shell;

namespace Dotnetstore.PersonalApp.Modules.Inventory;

/// <summary>The "Warehouse" navigation group that all pages of the module share.</summary>
internal static class InventoryGroup
{
    public const string ModuleKey = "inventory";

    public static ModulePageGroup Create() => new(ModuleKey, Localizer.Instance.Get("Inventory_Title"), "M22 21V7L12 3 2 7v14h5v-9h10v9h5zm-11-2H9v2h2v-2zm4 0h-2v2h2v-2z", 1);
}
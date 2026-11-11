using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Shell;

namespace Dotnetstore.PersonalApp.Modules.Sales;

/// <summary>The navigation group shared by the offers and customers pages.</summary>
internal static class SalesNav
{
    // Material "description" icon
    private const string IconData =
        "M14 2H6c-1.1 0-1.99.9-1.99 2L4 20c0 1.1.89 2 1.99 2H18c1.1 0 2-.9 2-2V8l-6-6zm2 16H8v-2h8v2zm0-4H8v-2h8v2zm-3-5V3.5L18.5 9H13z";

    public static ModulePageGroup Group() => new("sales", Localizer.Instance.Get("Sales_Title"), IconData);
}
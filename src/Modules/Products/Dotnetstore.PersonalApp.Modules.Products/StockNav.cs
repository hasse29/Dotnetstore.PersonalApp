using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Shell;

namespace Dotnetstore.PersonalApp.Modules.Products;

/// <summary>The navigation group shared by the stock, movements, counts and storage location pages.</summary>
internal static class StockNav
{
    // Material "warehouse" icon
    private const string IconData = "M22 21V7L12 3 2 7v14h5v-9h10v9h5zm-13 0v-2h6v2H9zm0-4v-2h6v2H9z";

    public static ModulePageGroup Group() => new("stock", Localizer.Instance.Get("Stock_Group"), IconData);
}
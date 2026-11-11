using Dotnetstore.PersonalApp.Modules.Products.Features.InventoryCounts;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;
using Dotnetstore.PersonalApp.Shared.Shell;
using Microsoft.Extensions.DependencyInjection;

namespace Dotnetstore.PersonalApp.Modules.Products;

internal sealed class InventoryCountsPage(IServiceProvider services) : IModulePage
{
    public string? ModuleKey => "products";

    public string Title => Localizer.Instance.Get("InventoryCounts_Title");

    // Material "fact check" icon
    public string IconData =>
        "M19 3H5c-1.1 0-2 .9-2 2v14c0 1.1.9 2 2 2h14c1.1 0 2-.9 2-2V5c0-1.1-.9-2-2-2zM9 17H7v-2h2v2zm0-4H7v-2h2v2zm0-4H7V7h2v2zm9 8h-7v-2h7v2zm0-4h-7v-2h7v2zm0-4h-7V7h7v2z";

    public ModulePageGroup? Group => StockNav.Group();

    public ViewModelBase CreateViewModel() => services.GetRequiredService<InventoryCountsViewModel>();
}
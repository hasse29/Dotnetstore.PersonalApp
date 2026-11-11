using Dotnetstore.PersonalApp.Modules.Products.Features.Stock;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;
using Dotnetstore.PersonalApp.Shared.Shell;
using Microsoft.Extensions.DependencyInjection;

namespace Dotnetstore.PersonalApp.Modules.Products;

internal sealed class StockMovementsPage(IServiceProvider services) : IModulePage
{
    public string? ModuleKey => "products";

    public string Title => Localizer.Instance.Get("StockMovements_Title");

    // Material "swap horiz" icon
    public string IconData =>
        "M6.99 11L3 15l3.99 4v-3H14v-2H6.99v-3zM21 9l-3.99-4v3H10v2h7.01v3L21 9z";

    public ModulePageGroup? Group => StockNav.Group();

    public ViewModelBase CreateViewModel() => services.GetRequiredService<StockMovementsViewModel>();
}
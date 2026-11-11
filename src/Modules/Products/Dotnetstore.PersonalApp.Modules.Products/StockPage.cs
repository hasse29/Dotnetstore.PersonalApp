using Dotnetstore.PersonalApp.Modules.Products.Features.Stock;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;
using Dotnetstore.PersonalApp.Shared.Shell;
using Microsoft.Extensions.DependencyInjection;

namespace Dotnetstore.PersonalApp.Modules.Products;

internal sealed class StockPage(IServiceProvider services) : IModulePage
{
    public string? ModuleKey => "products";

    public string Title => Localizer.Instance.Get("Stock_Title");

    // Material "inventory 2" style box icon
    public string IconData =>
        "M20 2H4c-1 0-2 .9-2 2v3.01c0 .72.43 1.34 1 1.69V20c0 1.1 1.1 2 2 2h14c.9 0 2-.9 2-2V8.7c.57-.35 1-.97 1-1.69V4c0-1.1-1-2-2-2zm-5 12H9v-2h6v2zm5-7H4V4h16v3z";

    public ModulePageGroup? Group => StockNav.Group();

    public ViewModelBase CreateViewModel() => services.GetRequiredService<StockViewModel>();
}
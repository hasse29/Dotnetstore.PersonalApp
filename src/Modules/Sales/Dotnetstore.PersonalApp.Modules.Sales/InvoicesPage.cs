using Dotnetstore.PersonalApp.Modules.Sales.Features.Invoices;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;
using Dotnetstore.PersonalApp.Shared.Shell;
using Microsoft.Extensions.DependencyInjection;

namespace Dotnetstore.PersonalApp.Modules.Sales;

internal sealed class InvoicesPage(IServiceProvider services) : IModulePage
{
    public string? ModuleKey => "sales";

    public string Title => Localizer.Instance.Get("Invoices_Title");

    // Material "receipt long" icon
    public string IconData =>
        "M19.5 3.5 18 2l-1.5 1.5L15 2l-1.5 1.5L12 2l-1.5 1.5L9 2 7.5 3.5 6 2v14H3v3c0 1.66 1.34 3 3 3h12c1.66 0 3-1.34 3-3V2l-1.5 1.5zM19 19c0 .55-.45 1-1 1s-1-.45-1-1v-3H8V5h11v14zM9 7h6v2H9V7zm7 0h2v2h-2V7zM9 10h6v2H9v-2zm7 0h2v2h-2v-2z";

    public ModulePageGroup? Group => SalesNav.Group();

    public ViewModelBase CreateViewModel() => services.GetRequiredService<InvoicesViewModel>();
}

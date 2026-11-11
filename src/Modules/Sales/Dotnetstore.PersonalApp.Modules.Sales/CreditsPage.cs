using Dotnetstore.PersonalApp.Modules.Sales.Features.Credits;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;
using Dotnetstore.PersonalApp.Shared.Shell;
using Microsoft.Extensions.DependencyInjection;

namespace Dotnetstore.PersonalApp.Modules.Sales;

internal sealed class CreditsPage(IServiceProvider services) : IModulePage
{
    public string? ModuleKey => "sales";

    public string Title => Localizer.Instance.Get("Credits_Title");

    // Material "payments" icon
    public string IconData =>
        "M19 14V6c0-1.1-.9-2-2-2H3c-1.1 0-2 .9-2 2v8c0 1.1.9 2 2 2h14c1.1 0 2-.9 2-2zm-9 1c-1.66 0-3-1.34-3-3s1.34-3 3-3 3 1.34 3 3-1.34 3-3 3zm13-6v11c0 1.1-.9 2-2 2H4v-2h17V9h2z";

    public ModulePageGroup? Group => SalesNav.Group();

    public ViewModelBase CreateViewModel() => services.GetRequiredService<CreditsViewModel>();
}

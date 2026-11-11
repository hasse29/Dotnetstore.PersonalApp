using Dotnetstore.PersonalApp.Modules.Sales.Features.PickLists;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;
using Dotnetstore.PersonalApp.Shared.Shell;
using Microsoft.Extensions.DependencyInjection;

namespace Dotnetstore.PersonalApp.Modules.Sales;

internal sealed class PickListsPage(IServiceProvider services) : IModulePage
{
    public string? ModuleKey => "sales";

    public string Title => Localizer.Instance.Get("PickLists_Title");

    // Material "assignment turned in" icon
    public string IconData =>
        "M19 3h-4.18C14.4 1.84 13.3 1 12 1s-2.4.84-2.82 2H5c-1.1 0-2 .9-2 2v14c0 1.1.9 2 2 2h14c1.1 0 2-.9 2-2V5c0-1.1-.9-2-2-2zm-7 0c.55 0 1 .45 1 1s-.45 1-1 1-1-.45-1-1 .45-1 1-1zm-2 14l-4-4 1.41-1.41L10 14.17l6.59-6.59L18 9l-8 8z";

    public ModulePageGroup? Group => SalesNav.Group();

    public ViewModelBase CreateViewModel() => services.GetRequiredService<PickListsViewModel>();
}
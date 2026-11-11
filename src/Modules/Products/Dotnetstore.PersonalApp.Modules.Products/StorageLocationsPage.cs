using Dotnetstore.PersonalApp.Modules.Products.Features.StorageLocations;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;
using Dotnetstore.PersonalApp.Shared.Shell;
using Microsoft.Extensions.DependencyInjection;

namespace Dotnetstore.PersonalApp.Modules.Products;

internal sealed class StorageLocationsPage(IServiceProvider services) : IModulePage
{
    public string? ModuleKey => "products";

    public string Title => Localizer.Instance.Get("StorageLocations_Title");

    // Material "warehouse" icon
    public string IconData =>
        "M22 21V7L12 3 2 7v14h5v-9h10v9h5zm-13 0v-2h6v2H9zm0-4v-2h6v2H9z";

    public ModulePageGroup? Group => StockNav.Group();

    public ViewModelBase CreateViewModel() => services.GetRequiredService<StorageLocationsViewModel>();
}
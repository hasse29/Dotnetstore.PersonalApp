using Dotnetstore.PersonalApp.Modules.Products.Features.ProductCategories;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;
using Dotnetstore.PersonalApp.Shared.Shell;
using Microsoft.Extensions.DependencyInjection;

namespace Dotnetstore.PersonalApp.Modules.Products;

internal sealed class ProductCategoriesPage(IServiceProvider services) : IModulePage
{
    public string? ModuleKey => "products";

    public string Title => Localizer.Instance.Get("ProductCategories_Title");

    // Material "label" icon
    public string IconData =>
        "M17.63 5.84C17.27 5.33 16.67 5 16 5L5 5.01C3.9 5.01 3 5.9 3 7v10c0 1.1.9 1.99 2 1.99L16 19c.67 0 1.27-.33 1.63-.84L22 12l-4.37-6.16z";

    public ModulePageGroup? Group => ProductsNav.Group();

    public ViewModelBase CreateViewModel() => services.GetRequiredService<ProductCategoriesViewModel>();
}

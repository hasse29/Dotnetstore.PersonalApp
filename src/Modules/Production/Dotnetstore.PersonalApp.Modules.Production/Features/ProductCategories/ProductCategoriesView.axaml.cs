using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Dotnetstore.PersonalApp.Modules.Production.Features.ProductCategories;

internal sealed partial class ProductCategoriesView : UserControl
{
    public ProductCategoriesView()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        if (DataContext is ProductCategoriesViewModel viewModel && viewModel.ProductCategories.Count == 0)
        {
            viewModel.LoadCommand.Execute(null);
        }
    }
}
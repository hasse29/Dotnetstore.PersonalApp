using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Dotnetstore.PersonalApp.Modules.Production.Features.ProductSubcategories;

internal sealed partial class ProductSubcategoriesView : UserControl
{
    public ProductSubcategoriesView()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        if (DataContext is ProductSubcategoriesViewModel viewModel && viewModel.ProductSubcategories.Count == 0)
        {
            viewModel.LoadCommand.Execute(null);
        }
    }
}
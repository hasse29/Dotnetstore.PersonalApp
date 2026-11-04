using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Dotnetstore.PersonalApp.Modules.Production.Features.Products;

internal sealed partial class ProductsView : UserControl
{
    public ProductsView()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        if (DataContext is ProductsViewModel viewModel && viewModel.Products.Count == 0)
        {
            viewModel.LoadCommand.Execute(null);
        }
    }
}
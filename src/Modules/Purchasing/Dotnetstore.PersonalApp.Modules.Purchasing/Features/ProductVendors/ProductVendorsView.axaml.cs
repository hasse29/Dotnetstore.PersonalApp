using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Dotnetstore.PersonalApp.Modules.Purchasing.Features.ProductVendors;

internal sealed partial class ProductVendorsView : UserControl
{
    public ProductVendorsView()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        if (DataContext is ProductVendorsViewModel viewModel && viewModel.ProductVendors.Count == 0)
        {
            viewModel.LoadCommand.Execute(null);
        }
    }
}
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Dotnetstore.PersonalApp.Modules.Products.Features.SupplierPrices;

internal sealed partial class SupplierPricesView : UserControl
{
    public SupplierPricesView()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        if (DataContext is SupplierPricesViewModel viewModel && viewModel.Prices.Count == 0)
        {
            viewModel.LoadCommand.Execute(null);
        }
    }
}

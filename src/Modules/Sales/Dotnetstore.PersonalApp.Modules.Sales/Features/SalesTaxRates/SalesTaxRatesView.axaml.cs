using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Dotnetstore.PersonalApp.Modules.Sales.Features.SalesTaxRates;

internal sealed partial class SalesTaxRatesView : UserControl
{
    public SalesTaxRatesView()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        if (DataContext is SalesTaxRatesViewModel viewModel && viewModel.SalesTaxRates.Count == 0)
        {
            viewModel.LoadCommand.Execute(null);
        }
    }
}
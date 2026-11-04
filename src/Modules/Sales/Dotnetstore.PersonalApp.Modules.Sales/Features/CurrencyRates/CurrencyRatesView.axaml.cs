using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Dotnetstore.PersonalApp.Modules.Sales.Features.CurrencyRates;

internal sealed partial class CurrencyRatesView : UserControl
{
    public CurrencyRatesView()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        if (DataContext is CurrencyRatesViewModel viewModel && viewModel.CurrencyRates.Count == 0)
        {
            viewModel.LoadCommand.Execute(null);
        }
    }
}
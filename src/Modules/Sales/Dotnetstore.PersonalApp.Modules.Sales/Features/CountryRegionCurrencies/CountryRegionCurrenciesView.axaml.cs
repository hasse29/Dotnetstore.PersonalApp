using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Dotnetstore.PersonalApp.Modules.Sales.Features.CountryRegionCurrencies;

internal sealed partial class CountryRegionCurrenciesView : UserControl
{
    public CountryRegionCurrenciesView()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        if (DataContext is CountryRegionCurrenciesViewModel viewModel && viewModel.CountryRegionCurrencies.Count == 0)
        {
            viewModel.LoadCommand.Execute(null);
        }
    }
}
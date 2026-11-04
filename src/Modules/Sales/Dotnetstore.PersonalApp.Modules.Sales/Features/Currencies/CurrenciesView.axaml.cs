using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Dotnetstore.PersonalApp.Modules.Sales.Features.Currencies;

internal sealed partial class CurrenciesView : UserControl
{
    public CurrenciesView()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        if (DataContext is CurrenciesViewModel viewModel && viewModel.Currencies.Count == 0)
        {
            viewModel.LoadCommand.Execute(null);
        }
    }
}
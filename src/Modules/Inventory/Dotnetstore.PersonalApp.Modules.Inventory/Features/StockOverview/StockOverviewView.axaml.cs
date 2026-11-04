using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Dotnetstore.PersonalApp.Modules.Inventory.Features.StockOverview;

internal sealed partial class StockOverviewView : UserControl
{
    public StockOverviewView()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        if (DataContext is StockOverviewViewModel viewModel && viewModel.Warehouses.Count == 0)
        {
            viewModel.LoadCommand.Execute(null);
        }
    }
}

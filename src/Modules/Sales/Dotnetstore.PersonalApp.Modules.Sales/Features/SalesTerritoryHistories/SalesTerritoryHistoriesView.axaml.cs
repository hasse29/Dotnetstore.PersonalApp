using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Dotnetstore.PersonalApp.Modules.Sales.Features.SalesTerritoryHistories;

internal sealed partial class SalesTerritoryHistoriesView : UserControl
{
    public SalesTerritoryHistoriesView()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        if (DataContext is SalesTerritoryHistoriesViewModel viewModel && viewModel.SalesTerritoryHistories.Count == 0)
        {
            viewModel.LoadCommand.Execute(null);
        }
    }
}
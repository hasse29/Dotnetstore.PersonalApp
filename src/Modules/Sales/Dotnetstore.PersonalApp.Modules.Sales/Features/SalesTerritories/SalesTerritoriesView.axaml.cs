using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Dotnetstore.PersonalApp.Modules.Sales.Features.SalesTerritories;

internal sealed partial class SalesTerritoriesView : UserControl
{
    public SalesTerritoriesView()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        if (DataContext is SalesTerritoriesViewModel viewModel && viewModel.SalesTerritories.Count == 0)
        {
            viewModel.LoadCommand.Execute(null);
        }
    }
}
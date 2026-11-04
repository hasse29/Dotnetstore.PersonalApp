using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Dotnetstore.PersonalApp.Modules.Inventory.Features.StockMovements;

internal sealed partial class StockMovementsView : UserControl
{
    public StockMovementsView()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        if (DataContext is StockMovementsViewModel viewModel && viewModel.StockMovements.Count == 0)
        {
            viewModel.LoadCommand.Execute(null);
        }
    }
}
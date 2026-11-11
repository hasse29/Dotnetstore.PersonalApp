using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Dotnetstore.PersonalApp.Modules.Products.Features.Stock;

internal sealed partial class StockMovementsView : UserControl
{
    public StockMovementsView()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        if (DataContext is StockMovementsViewModel viewModel && viewModel.Movements.Count == 0)
        {
            viewModel.LoadCommand.Execute(null);
        }
    }
}

using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Dotnetstore.PersonalApp.Modules.Inventory.Features.StockLevels;

internal sealed partial class StockLevelsView : UserControl
{
    public StockLevelsView()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        if (DataContext is StockLevelsViewModel viewModel && viewModel.StockLevels.Count == 0)
        {
            viewModel.LoadCommand.Execute(null);
        }
    }
}
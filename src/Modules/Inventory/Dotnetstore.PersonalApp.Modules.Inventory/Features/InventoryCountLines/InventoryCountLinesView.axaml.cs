using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Dotnetstore.PersonalApp.Modules.Inventory.Features.InventoryCountLines;

internal sealed partial class InventoryCountLinesView : UserControl
{
    public InventoryCountLinesView()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        if (DataContext is InventoryCountLinesViewModel viewModel && viewModel.InventoryCountLines.Count == 0)
        {
            viewModel.LoadCommand.Execute(null);
        }
    }
}
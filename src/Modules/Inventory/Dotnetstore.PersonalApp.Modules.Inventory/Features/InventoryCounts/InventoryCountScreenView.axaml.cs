using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Dotnetstore.PersonalApp.Modules.Inventory.Features.InventoryCounts;

internal sealed partial class InventoryCountScreenView : UserControl
{
    public InventoryCountScreenView()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        if (DataContext is InventoryCountScreenViewModel { Editor: null } viewModel)
        {
            viewModel.LoadCommand.Execute(null);
        }
    }
}

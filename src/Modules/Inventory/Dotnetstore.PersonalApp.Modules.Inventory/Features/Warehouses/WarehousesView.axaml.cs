using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Dotnetstore.PersonalApp.Modules.Inventory.Features.Warehouses;

internal sealed partial class WarehousesView : UserControl
{
    public WarehousesView()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        if (DataContext is WarehousesViewModel viewModel && viewModel.Warehouses.Count == 0)
        {
            viewModel.LoadCommand.Execute(null);
        }
    }
}
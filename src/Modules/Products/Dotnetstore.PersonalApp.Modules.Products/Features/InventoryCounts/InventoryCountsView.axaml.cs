using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Dotnetstore.PersonalApp.Modules.Products.Features.InventoryCounts;

internal sealed partial class InventoryCountsView : UserControl
{
    public InventoryCountsView()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        if (DataContext is InventoryCountsViewModel viewModel && viewModel.Counts.Count == 0)
        {
            viewModel.LoadCommand.Execute(null);
        }
    }
}

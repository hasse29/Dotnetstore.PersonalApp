using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Dotnetstore.PersonalApp.Modules.Products.Features.StorageLocations;

internal sealed partial class StorageLocationsView : UserControl
{
    public StorageLocationsView()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        if (DataContext is StorageLocationsViewModel viewModel && viewModel.Locations.Count == 0)
        {
            viewModel.LoadCommand.Execute(null);
        }
    }
}

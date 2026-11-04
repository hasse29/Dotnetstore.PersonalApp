using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Dotnetstore.PersonalApp.Modules.Production.Features.Locations;

internal sealed partial class LocationsView : UserControl
{
    public LocationsView()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        if (DataContext is LocationsViewModel viewModel && viewModel.Locations.Count == 0)
        {
            viewModel.LoadCommand.Execute(null);
        }
    }
}
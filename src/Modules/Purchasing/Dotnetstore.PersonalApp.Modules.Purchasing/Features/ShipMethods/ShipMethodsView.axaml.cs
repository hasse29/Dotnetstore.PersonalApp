using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Dotnetstore.PersonalApp.Modules.Purchasing.Features.ShipMethods;

internal sealed partial class ShipMethodsView : UserControl
{
    public ShipMethodsView()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        if (DataContext is ShipMethodsViewModel viewModel && viewModel.ShipMethods.Count == 0)
        {
            viewModel.LoadCommand.Execute(null);
        }
    }
}
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Dotnetstore.PersonalApp.Modules.Addresses.Features.Addresses;

internal sealed partial class ObjectAddressesView : UserControl
{
    public ObjectAddressesView()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        if (DataContext is ObjectAddressesViewModel viewModel && viewModel.Addresses.Count == 0)
        {
            viewModel.LoadCommand.Execute(null);
        }
    }
}

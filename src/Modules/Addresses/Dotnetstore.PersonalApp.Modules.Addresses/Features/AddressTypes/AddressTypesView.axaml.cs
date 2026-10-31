using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Dotnetstore.PersonalApp.Modules.Addresses.Features.AddressTypes;

internal sealed partial class AddressTypesView : UserControl
{
    public AddressTypesView()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        if (DataContext is AddressTypesViewModel viewModel && viewModel.AddressTypes.Count == 0)
        {
            viewModel.LoadCommand.Execute(null);
        }
    }
}

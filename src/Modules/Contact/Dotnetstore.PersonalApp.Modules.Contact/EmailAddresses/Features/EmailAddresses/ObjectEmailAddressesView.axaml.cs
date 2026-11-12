using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Dotnetstore.PersonalApp.Modules.Contact.EmailAddresses.Features.EmailAddresses;

internal sealed partial class ObjectEmailAddressesView : UserControl
{
    public ObjectEmailAddressesView()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        if (DataContext is ObjectEmailAddressesViewModel viewModel && viewModel.EmailAddresses.Count == 0)
        {
            viewModel.LoadCommand.Execute(null);
        }
    }
}

using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Dotnetstore.PersonalApp.Modules.EmailAddresses.Features.EmailAddressTypes;

internal sealed partial class EmailAddressTypesView : UserControl
{
    public EmailAddressTypesView()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        if (DataContext is EmailAddressTypesViewModel viewModel && viewModel.EmailAddressTypes.Count == 0)
        {
            viewModel.LoadCommand.Execute(null);
        }
    }
}

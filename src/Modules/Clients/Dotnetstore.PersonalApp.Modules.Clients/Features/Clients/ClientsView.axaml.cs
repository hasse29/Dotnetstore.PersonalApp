using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Dotnetstore.PersonalApp.Modules.Clients.Features.Clients;

internal sealed partial class ClientsView : UserControl
{
    public ClientsView()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        if (DataContext is ClientsViewModel viewModel && viewModel.Clients.Count == 0)
        {
            viewModel.LoadCommand.Execute(null);
        }
    }
}

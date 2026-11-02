using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Dotnetstore.PersonalApp.Modules.Clients.Features.ContactPersons;

internal sealed partial class ContactPersonsView : UserControl
{
    public ContactPersonsView()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        if (DataContext is ContactPersonsViewModel viewModel && viewModel.People.Count == 0)
        {
            viewModel.LoadCommand.Execute(null);
        }
    }
}

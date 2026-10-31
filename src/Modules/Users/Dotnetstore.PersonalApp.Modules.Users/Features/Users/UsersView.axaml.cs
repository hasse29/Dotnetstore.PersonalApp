using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Dotnetstore.PersonalApp.Modules.Users.Features.Users;

internal sealed partial class UsersView : UserControl
{
    public UsersView()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        if (DataContext is UsersViewModel viewModel && viewModel.Users.Count == 0)
        {
            viewModel.LoadCommand.Execute(null);
        }
    }
}

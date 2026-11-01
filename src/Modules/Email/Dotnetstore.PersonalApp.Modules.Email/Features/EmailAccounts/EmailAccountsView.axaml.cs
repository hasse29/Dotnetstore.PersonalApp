using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Dotnetstore.PersonalApp.Modules.Email.Features.EmailAccounts;

internal sealed partial class EmailAccountsView : UserControl
{
    public EmailAccountsView()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        if (DataContext is EmailAccountsViewModel viewModel && viewModel.Accounts.Count == 0)
        {
            viewModel.LoadCommand.Execute(null);
        }
    }
}

using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Dotnetstore.PersonalApp.Modules.Email.Features.Messages;

internal sealed partial class ComposeView : UserControl
{
    public ComposeView()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        if (DataContext is ComposeViewModel viewModel && viewModel.Accounts.Count == 0)
        {
            viewModel.LoadCommand.Execute(null);
        }
    }
}

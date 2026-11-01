using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Dotnetstore.PersonalApp.Modules.Email.Features.Messages;

internal sealed partial class SentView : UserControl
{
    public SentView()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        if (DataContext is SentViewModel viewModel && viewModel.Accounts.Count == 0)
        {
            viewModel.LoadCommand.Execute(null);
        }
    }
}

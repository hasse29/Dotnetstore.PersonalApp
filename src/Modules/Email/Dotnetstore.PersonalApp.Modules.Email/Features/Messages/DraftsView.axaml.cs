using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Dotnetstore.PersonalApp.Modules.Email.Features.Messages;

internal sealed partial class DraftsView : UserControl
{
    public DraftsView()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        if (DataContext is DraftsViewModel viewModel)
        {
            viewModel.LoadCommand.Execute(null);
        }
    }
}

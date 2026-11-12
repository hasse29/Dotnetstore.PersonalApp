using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Dotnetstore.PersonalApp.Modules.Audit.Features.Entries;

internal sealed partial class AuditView : UserControl
{
    public AuditView()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        if (DataContext is AuditViewModel viewModel && viewModel.Entries.Count == 0)
        {
            viewModel.LoadCommand.Execute(null);
        }
    }
}

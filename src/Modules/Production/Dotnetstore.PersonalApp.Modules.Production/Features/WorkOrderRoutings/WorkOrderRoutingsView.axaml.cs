using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Dotnetstore.PersonalApp.Modules.Production.Features.WorkOrderRoutings;

internal sealed partial class WorkOrderRoutingsView : UserControl
{
    public WorkOrderRoutingsView()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        if (DataContext is WorkOrderRoutingsViewModel viewModel && viewModel.WorkOrderRoutings.Count == 0)
        {
            viewModel.LoadCommand.Execute(null);
        }
    }
}
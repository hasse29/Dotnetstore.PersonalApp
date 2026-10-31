using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Dotnetstore.PersonalApp.Modules.TimeReports.Features.WorkTimeTypes;

internal sealed partial class WorkTimeTypesView : UserControl
{
    public WorkTimeTypesView()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        if (DataContext is WorkTimeTypesViewModel viewModel && viewModel.WorkTimeTypes.Count == 0)
        {
            viewModel.LoadCommand.Execute(null);
        }
    }
}

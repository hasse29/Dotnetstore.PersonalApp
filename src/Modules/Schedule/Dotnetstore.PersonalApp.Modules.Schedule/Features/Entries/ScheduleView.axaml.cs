using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Dotnetstore.PersonalApp.Modules.Schedule.Features.Entries;

internal sealed partial class ScheduleView : UserControl
{
    public ScheduleView()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        if (DataContext is ScheduleViewModel viewModel && viewModel.Rows.Count == 0)
        {
            viewModel.LoadCommand.Execute(null);
        }
    }
}

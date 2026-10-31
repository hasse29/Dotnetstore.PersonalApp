using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Dotnetstore.PersonalApp.Modules.TimeReports.Features.TimeReports;

internal sealed partial class TimeReportsView : UserControl
{
    public TimeReportsView()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        if (DataContext is TimeReportsViewModel viewModel && viewModel.Reports.Count == 0)
        {
            viewModel.LoadCommand.Execute(null);
        }
    }
}
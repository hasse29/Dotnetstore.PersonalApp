using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Dotnetstore.PersonalApp.Modules.Grades.Features.Tests;

internal sealed partial class TestResultsView : UserControl
{
    public TestResultsView()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        if (DataContext is TestResultsViewModel viewModel)
        {
            viewModel.LoadCommand.Execute(null);
        }
    }
}

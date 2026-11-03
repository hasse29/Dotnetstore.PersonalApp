using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Dotnetstore.PersonalApp.Modules.Grades.Features.Tests;

internal sealed partial class TestsView : UserControl
{
    public TestsView()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        if (DataContext is TestsViewModel viewModel)
        {
            viewModel.LoadCommand.Execute(null);
        }
    }
}

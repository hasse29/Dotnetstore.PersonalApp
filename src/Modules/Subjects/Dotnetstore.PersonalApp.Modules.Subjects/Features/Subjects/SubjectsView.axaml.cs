using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Dotnetstore.PersonalApp.Modules.Subjects.Features.Subjects;

internal sealed partial class SubjectsView : UserControl
{
    public SubjectsView()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        if (DataContext is SubjectsViewModel viewModel && viewModel.Subjects.Count == 0)
        {
            viewModel.LoadCommand.Execute(null);
        }
    }
}

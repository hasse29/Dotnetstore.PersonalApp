using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Dotnetstore.PersonalApp.Modules.Subjects.Features.TeacherSubjects;

internal sealed partial class UserSubjectsView : UserControl
{
    public UserSubjectsView()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        if (DataContext is UserSubjectsViewModel viewModel && viewModel.Taught.Count == 0)
        {
            viewModel.LoadCommand.Execute(null);
        }
    }
}

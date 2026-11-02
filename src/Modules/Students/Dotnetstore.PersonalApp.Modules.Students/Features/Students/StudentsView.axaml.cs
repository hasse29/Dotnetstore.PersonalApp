using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Dotnetstore.PersonalApp.Modules.Students.Features.Students;

internal sealed partial class StudentsView : UserControl
{
    public StudentsView()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        if (DataContext is StudentsViewModel viewModel && viewModel.Students.Count == 0)
        {
            viewModel.LoadCommand.Execute(null);
        }
    }
}

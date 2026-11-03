using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Dotnetstore.PersonalApp.Modules.Grades.Features.Grades;

internal sealed partial class GradesView : UserControl
{
    public GradesView()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        if (DataContext is GradesViewModel viewModel)
        {
            viewModel.LoadCommand.Execute(null);
        }
    }
}

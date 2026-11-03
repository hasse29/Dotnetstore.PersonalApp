using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Dotnetstore.PersonalApp.Modules.Grades.Features.Scales;

internal sealed partial class GradeScalesView : UserControl
{
    public GradeScalesView()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        if (DataContext is GradeScalesViewModel viewModel)
        {
            viewModel.LoadCommand.Execute(null);
        }
    }
}

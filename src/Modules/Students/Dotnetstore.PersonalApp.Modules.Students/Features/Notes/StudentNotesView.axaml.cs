using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Dotnetstore.PersonalApp.Modules.Students.Features.Notes;

internal sealed partial class StudentNotesView : UserControl
{
    public StudentNotesView()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        if (DataContext is StudentNotesViewModel viewModel)
        {
            viewModel.LoadCommand.Execute(null);
        }
    }
}

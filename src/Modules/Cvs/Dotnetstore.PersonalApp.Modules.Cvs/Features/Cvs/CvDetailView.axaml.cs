using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Dotnetstore.PersonalApp.Modules.Cvs.Features.Cvs;

internal sealed partial class CvDetailView : UserControl
{
    public CvDetailView()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        if (DataContext is CvDetailViewModel viewModel)
        {
            viewModel.LoadCommand.Execute(null);
        }
    }
}

using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Dotnetstore.PersonalApp.Modules.Cvs.Features.Cvs;

internal sealed partial class CvsView : UserControl
{
    public CvsView()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        if (DataContext is CvsViewModel viewModel && viewModel.Cvs.Count == 0)
        {
            viewModel.LoadCommand.Execute(null);
        }
    }
}

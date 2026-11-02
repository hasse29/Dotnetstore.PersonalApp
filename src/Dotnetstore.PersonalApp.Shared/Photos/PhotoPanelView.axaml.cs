using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Dotnetstore.PersonalApp.Shared.Photos;

public sealed partial class PhotoPanelView : UserControl
{
    public PhotoPanelView()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        if (DataContext is PhotoPanelViewModel viewModel)
        {
            viewModel.LoadCommand.Execute(null);
        }
    }
}

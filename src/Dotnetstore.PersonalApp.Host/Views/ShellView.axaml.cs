using Avalonia.Controls;
using Avalonia.Interactivity;
using Dotnetstore.PersonalApp.Host.ViewModels;

namespace Dotnetstore.PersonalApp.Host.Views;

internal sealed partial class ShellView : UserControl
{
    public ShellView()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        if (DataContext is ShellViewModel viewModel)
        {
            viewModel.LoadUserPhotoCommand.Execute(null);
        }
    }
}

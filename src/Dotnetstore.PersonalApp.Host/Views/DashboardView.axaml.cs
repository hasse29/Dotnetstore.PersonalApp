using Avalonia.Controls;
using Avalonia.Interactivity;
using Dotnetstore.PersonalApp.Host.ViewModels;

namespace Dotnetstore.PersonalApp.Host.Views;

internal sealed partial class DashboardView : UserControl
{
    public DashboardView()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        if (DataContext is DashboardViewModel viewModel)
        {
            viewModel.LoadCommand.Execute(null);
        }
    }
}

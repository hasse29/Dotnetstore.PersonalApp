using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Dotnetstore.PersonalApp.Modules.Sales.Features.Credits;

internal sealed partial class CreditsView : UserControl
{
    public CreditsView()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        if (DataContext is CreditsViewModel viewModel)
        {
            viewModel.LoadCommand.Execute(null);
        }
    }
}

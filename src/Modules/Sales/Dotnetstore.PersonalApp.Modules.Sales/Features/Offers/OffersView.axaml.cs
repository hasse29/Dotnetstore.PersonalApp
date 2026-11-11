using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Dotnetstore.PersonalApp.Modules.Sales.Features.Offers;

internal sealed partial class OffersView : UserControl
{
    public OffersView()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        if (DataContext is OffersViewModel viewModel && viewModel.Offers.Count == 0)
        {
            viewModel.LoadCommand.Execute(null);
        }
    }
}

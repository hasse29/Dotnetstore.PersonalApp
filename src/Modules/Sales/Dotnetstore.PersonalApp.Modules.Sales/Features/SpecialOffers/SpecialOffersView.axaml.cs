using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Dotnetstore.PersonalApp.Modules.Sales.Features.SpecialOffers;

internal sealed partial class SpecialOffersView : UserControl
{
    public SpecialOffersView()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        if (DataContext is SpecialOffersViewModel viewModel && viewModel.SpecialOffers.Count == 0)
        {
            viewModel.LoadCommand.Execute(null);
        }
    }
}
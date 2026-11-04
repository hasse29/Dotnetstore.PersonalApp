using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Dotnetstore.PersonalApp.Modules.Sales.Features.SpecialOfferProducts;

internal sealed partial class SpecialOfferProductsView : UserControl
{
    public SpecialOfferProductsView()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        if (DataContext is SpecialOfferProductsViewModel viewModel && viewModel.SpecialOfferProducts.Count == 0)
        {
            viewModel.LoadCommand.Execute(null);
        }
    }
}
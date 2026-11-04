using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Dotnetstore.PersonalApp.Modules.Production.Features.ProductListPriceHistories;

internal sealed partial class ProductListPriceHistoriesView : UserControl
{
    public ProductListPriceHistoriesView()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        if (DataContext is ProductListPriceHistoriesViewModel viewModel && viewModel.ProductListPriceHistories.Count == 0)
        {
            viewModel.LoadCommand.Execute(null);
        }
    }
}
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Dotnetstore.PersonalApp.Modules.Production.Features.ProductCostHistories;

internal sealed partial class ProductCostHistoriesView : UserControl
{
    public ProductCostHistoriesView()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        if (DataContext is ProductCostHistoriesViewModel viewModel && viewModel.ProductCostHistories.Count == 0)
        {
            viewModel.LoadCommand.Execute(null);
        }
    }
}
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Dotnetstore.PersonalApp.Modules.Products.Features.Stock;

internal sealed partial class ProductStockView : UserControl
{
    public ProductStockView()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        if (DataContext is ProductStockViewModel viewModel)
        {
            viewModel.LoadCommand.Execute(null);
        }
    }
}

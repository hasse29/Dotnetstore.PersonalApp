using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Dotnetstore.PersonalApp.Modules.Products.Features.Stock;

internal sealed partial class StockView : UserControl
{
    public StockView()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        if (DataContext is StockViewModel viewModel && viewModel.Rows.Count == 0)
        {
            viewModel.LoadCommand.Execute(null);
        }
    }
}

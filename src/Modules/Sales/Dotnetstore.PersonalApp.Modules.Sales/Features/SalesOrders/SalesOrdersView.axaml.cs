using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Dotnetstore.PersonalApp.Modules.Sales.Features.SalesOrders;

internal sealed partial class SalesOrdersView : UserControl
{
    public SalesOrdersView()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        if (DataContext is SalesOrdersViewModel viewModel && viewModel.SalesOrders.Count == 0)
        {
            viewModel.LoadCommand.Execute(null);
        }
    }
}
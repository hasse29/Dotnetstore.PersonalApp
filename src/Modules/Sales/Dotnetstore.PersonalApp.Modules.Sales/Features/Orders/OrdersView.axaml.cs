using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Dotnetstore.PersonalApp.Modules.Sales.Features.Orders;

internal sealed partial class OrdersView : UserControl
{
    public OrdersView()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        if (DataContext is OrdersViewModel viewModel && viewModel.Orders.Count == 0)
        {
            viewModel.LoadCommand.Execute(null);
        }
    }
}

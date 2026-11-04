using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Dotnetstore.PersonalApp.Modules.Purchasing.Features.PurchaseOrders;

internal sealed partial class PurchaseOrdersView : UserControl
{
    public PurchaseOrdersView()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        if (DataContext is PurchaseOrdersViewModel viewModel && viewModel.PurchaseOrders.Count == 0)
        {
            viewModel.LoadCommand.Execute(null);
        }
    }
}
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Dotnetstore.PersonalApp.Modules.Purchasing.Features.PurchaseOrders;

internal sealed partial class PurchaseOrderScreenView : UserControl
{
    public PurchaseOrderScreenView()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        if (DataContext is PurchaseOrderScreenViewModel { Editor: null } viewModel)
        {
            viewModel.LoadCommand.Execute(null);
        }
    }
}

using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Dotnetstore.PersonalApp.Modules.Purchasing.Features.PurchaseOrderDetails;

internal sealed partial class PurchaseOrderDetailsView : UserControl
{
    public PurchaseOrderDetailsView()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        if (DataContext is PurchaseOrderDetailsViewModel viewModel && viewModel.PurchaseOrderDetails.Count == 0)
        {
            viewModel.LoadCommand.Execute(null);
        }
    }
}
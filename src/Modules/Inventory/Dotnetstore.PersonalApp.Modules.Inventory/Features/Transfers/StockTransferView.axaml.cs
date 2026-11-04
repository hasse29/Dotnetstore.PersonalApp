using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Dotnetstore.PersonalApp.Modules.Inventory.Features.Transfers;

internal sealed partial class StockTransferView : UserControl
{
    public StockTransferView()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        if (DataContext is StockTransferViewModel viewModel)
        {
            viewModel.LoadCommand.Execute(null);
        }
    }
}

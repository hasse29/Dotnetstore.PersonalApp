using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Dotnetstore.PersonalApp.Modules.Inventory.Features.Receiving;

internal sealed partial class GoodsMovementPanel : UserControl
{
    public GoodsMovementPanel()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        if (DataContext is GoodsMovementViewModel viewModel)
        {
            viewModel.LoadCommand.Execute(null);
        }
    }
}

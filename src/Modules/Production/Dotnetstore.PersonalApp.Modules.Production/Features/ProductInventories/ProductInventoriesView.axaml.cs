using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Dotnetstore.PersonalApp.Modules.Production.Features.ProductInventories;

internal sealed partial class ProductInventoriesView : UserControl
{
    public ProductInventoriesView()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        if (DataContext is ProductInventoriesViewModel viewModel && viewModel.ProductInventories.Count == 0)
        {
            viewModel.LoadCommand.Execute(null);
        }
    }
}
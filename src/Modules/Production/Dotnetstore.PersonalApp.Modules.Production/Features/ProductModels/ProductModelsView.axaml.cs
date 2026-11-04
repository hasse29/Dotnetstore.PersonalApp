using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Dotnetstore.PersonalApp.Modules.Production.Features.ProductModels;

internal sealed partial class ProductModelsView : UserControl
{
    public ProductModelsView()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        if (DataContext is ProductModelsViewModel viewModel && viewModel.ProductModels.Count == 0)
        {
            viewModel.LoadCommand.Execute(null);
        }
    }
}
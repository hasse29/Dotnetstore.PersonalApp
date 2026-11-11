using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Dotnetstore.PersonalApp.Modules.Products.Features.Suppliers;

internal sealed partial class SuppliersView : UserControl
{
    public SuppliersView()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        if (DataContext is SuppliersViewModel viewModel && viewModel.Suppliers.Count == 0)
        {
            viewModel.LoadCommand.Execute(null);
        }
    }
}

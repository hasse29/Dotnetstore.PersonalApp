using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Dotnetstore.PersonalApp.Modules.Production.Features.BillOfMaterials;

internal sealed partial class BillOfMaterialsView : UserControl
{
    public BillOfMaterialsView()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        if (DataContext is BillOfMaterialsViewModel viewModel && viewModel.BillOfMaterials.Count == 0)
        {
            viewModel.LoadCommand.Execute(null);
        }
    }
}
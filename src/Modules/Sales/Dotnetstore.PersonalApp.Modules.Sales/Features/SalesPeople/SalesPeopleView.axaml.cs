using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Dotnetstore.PersonalApp.Modules.Sales.Features.SalesPeople;

internal sealed partial class SalesPeopleView : UserControl
{
    public SalesPeopleView()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        if (DataContext is SalesPeopleViewModel viewModel && viewModel.SalesPeople.Count == 0)
        {
            viewModel.LoadCommand.Execute(null);
        }
    }
}
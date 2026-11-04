using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Dotnetstore.PersonalApp.Modules.Sales.Features.SalesOrders;

internal sealed partial class SalesOrderScreenView : UserControl
{
    public SalesOrderScreenView()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        if (DataContext is SalesOrderScreenViewModel { Editor: null } viewModel)
        {
            viewModel.LoadCommand.Execute(null);
        }
    }
}

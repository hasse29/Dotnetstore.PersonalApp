using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Dotnetstore.PersonalApp.Modules.Sales.Features.SalesOrderDetails;

internal sealed partial class SalesOrderDetailsView : UserControl
{
    public SalesOrderDetailsView()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        if (DataContext is SalesOrderDetailsViewModel viewModel && viewModel.SalesOrderDetails.Count == 0)
        {
            viewModel.LoadCommand.Execute(null);
        }
    }
}
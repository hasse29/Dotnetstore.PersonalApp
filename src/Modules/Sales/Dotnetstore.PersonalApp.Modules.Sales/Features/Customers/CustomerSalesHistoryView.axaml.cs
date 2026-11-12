using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Dotnetstore.PersonalApp.Modules.Sales.Features.Customers;

internal sealed partial class CustomerSalesHistoryView : UserControl
{
    public CustomerSalesHistoryView()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        if (DataContext is CustomerSalesHistoryViewModel viewModel)
        {
            viewModel.LoadCommand.Execute(null);
        }
    }
}

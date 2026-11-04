using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Dotnetstore.PersonalApp.Modules.Sales.Features.Customers;

internal sealed partial class CustomersView : UserControl
{
    public CustomersView()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        if (DataContext is CustomersViewModel viewModel && viewModel.Customers.Count == 0)
        {
            viewModel.LoadCommand.Execute(null);
        }
    }
}
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Dotnetstore.PersonalApp.Modules.Sales.Features.Invoices;

internal sealed partial class InvoicesView : UserControl
{
    public InvoicesView()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        if (DataContext is InvoicesViewModel viewModel && viewModel.Invoices.Count == 0)
        {
            viewModel.LoadCommand.Execute(null);
        }
    }
}

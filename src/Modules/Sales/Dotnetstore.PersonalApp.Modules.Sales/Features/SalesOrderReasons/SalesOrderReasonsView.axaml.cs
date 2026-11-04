using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Dotnetstore.PersonalApp.Modules.Sales.Features.SalesOrderReasons;

internal sealed partial class SalesOrderReasonsView : UserControl
{
    public SalesOrderReasonsView()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        if (DataContext is SalesOrderReasonsViewModel viewModel && viewModel.SalesOrderReasons.Count == 0)
        {
            viewModel.LoadCommand.Execute(null);
        }
    }
}
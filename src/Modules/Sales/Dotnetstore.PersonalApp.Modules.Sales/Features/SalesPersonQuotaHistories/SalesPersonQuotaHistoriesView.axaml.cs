using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Dotnetstore.PersonalApp.Modules.Sales.Features.SalesPersonQuotaHistories;

internal sealed partial class SalesPersonQuotaHistoriesView : UserControl
{
    public SalesPersonQuotaHistoriesView()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        if (DataContext is SalesPersonQuotaHistoriesViewModel viewModel && viewModel.SalesPersonQuotaHistories.Count == 0)
        {
            viewModel.LoadCommand.Execute(null);
        }
    }
}
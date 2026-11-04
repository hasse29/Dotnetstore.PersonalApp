using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Dotnetstore.PersonalApp.Modules.Production.Features.WorkOrders;

internal sealed partial class WorkOrdersView : UserControl
{
    public WorkOrdersView()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        if (DataContext is WorkOrdersViewModel viewModel && viewModel.WorkOrders.Count == 0)
        {
            viewModel.LoadCommand.Execute(null);
        }
    }
}
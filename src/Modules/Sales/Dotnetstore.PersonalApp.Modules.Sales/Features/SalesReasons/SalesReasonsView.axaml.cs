using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Dotnetstore.PersonalApp.Modules.Sales.Features.SalesReasons;

internal sealed partial class SalesReasonsView : UserControl
{
    public SalesReasonsView()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        if (DataContext is SalesReasonsViewModel viewModel && viewModel.SalesReasons.Count == 0)
        {
            viewModel.LoadCommand.Execute(null);
        }
    }
}
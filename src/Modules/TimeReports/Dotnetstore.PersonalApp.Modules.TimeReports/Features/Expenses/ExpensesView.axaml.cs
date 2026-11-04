using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Dotnetstore.PersonalApp.Modules.TimeReports.Features.Expenses;

internal sealed partial class ExpensesView : UserControl
{
    public ExpensesView()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        if (DataContext is ExpensesViewModel viewModel && viewModel.Expenses.Count == 0)
        {
            viewModel.LoadCommand.Execute(null);
        }
    }
}

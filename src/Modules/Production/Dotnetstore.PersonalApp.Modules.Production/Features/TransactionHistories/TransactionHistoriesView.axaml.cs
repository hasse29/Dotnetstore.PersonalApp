using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Dotnetstore.PersonalApp.Modules.Production.Features.TransactionHistories;

internal sealed partial class TransactionHistoriesView : UserControl
{
    public TransactionHistoriesView()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        if (DataContext is TransactionHistoriesViewModel viewModel && viewModel.TransactionHistories.Count == 0)
        {
            viewModel.LoadCommand.Execute(null);
        }
    }
}
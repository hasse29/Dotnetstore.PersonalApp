using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Dotnetstore.PersonalApp.Modules.Sales.Features.PickLists;

internal sealed partial class PickListsView : UserControl
{
    public PickListsView()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        if (DataContext is PickListsViewModel viewModel && viewModel.PickLists.Count == 0)
        {
            viewModel.LoadCommand.Execute(null);
        }
    }
}

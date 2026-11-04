using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Dotnetstore.PersonalApp.Modules.Sales.Features.Stores;

internal sealed partial class StoresView : UserControl
{
    public StoresView()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        if (DataContext is StoresViewModel viewModel && viewModel.Stores.Count == 0)
        {
            viewModel.LoadCommand.Execute(null);
        }
    }
}
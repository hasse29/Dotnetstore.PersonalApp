using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Dotnetstore.PersonalApp.Modules.Purchasing.Features.Vendors;

internal sealed partial class VendorsView : UserControl
{
    public VendorsView()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        if (DataContext is VendorsViewModel viewModel && viewModel.Vendors.Count == 0)
        {
            viewModel.LoadCommand.Execute(null);
        }
    }
}
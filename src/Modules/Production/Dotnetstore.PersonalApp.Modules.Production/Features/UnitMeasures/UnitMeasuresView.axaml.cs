using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Dotnetstore.PersonalApp.Modules.Production.Features.UnitMeasures;

internal sealed partial class UnitMeasuresView : UserControl
{
    public UnitMeasuresView()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        if (DataContext is UnitMeasuresViewModel viewModel && viewModel.UnitMeasures.Count == 0)
        {
            viewModel.LoadCommand.Execute(null);
        }
    }
}
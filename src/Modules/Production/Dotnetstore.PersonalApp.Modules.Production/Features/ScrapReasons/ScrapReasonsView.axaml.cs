using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Dotnetstore.PersonalApp.Modules.Production.Features.ScrapReasons;

internal sealed partial class ScrapReasonsView : UserControl
{
    public ScrapReasonsView()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        if (DataContext is ScrapReasonsViewModel viewModel && viewModel.ScrapReasons.Count == 0)
        {
            viewModel.LoadCommand.Execute(null);
        }
    }
}
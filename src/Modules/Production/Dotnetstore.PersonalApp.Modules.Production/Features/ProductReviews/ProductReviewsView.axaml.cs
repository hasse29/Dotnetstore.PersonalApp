using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Dotnetstore.PersonalApp.Modules.Production.Features.ProductReviews;

internal sealed partial class ProductReviewsView : UserControl
{
    public ProductReviewsView()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        if (DataContext is ProductReviewsViewModel viewModel && viewModel.ProductReviews.Count == 0)
        {
            viewModel.LoadCommand.Execute(null);
        }
    }
}
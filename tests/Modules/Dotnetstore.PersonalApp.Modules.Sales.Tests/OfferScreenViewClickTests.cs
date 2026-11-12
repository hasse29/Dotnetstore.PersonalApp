using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Dotnetstore.PersonalApp.Modules.Sales.Features.Offers;
using Dotnetstore.PersonalApp.Shared.Products;

namespace Dotnetstore.PersonalApp.Modules.Sales.Tests;

/// <summary>Clicks a search result in a real, headless window. This passes with and without the deferral in the view model, so it covers the click path but did not reproduce the crash.</summary>
public sealed class OfferScreenViewClickTests : IDisposable
{
    private readonly ScreenHarness _harness = new();

    public void Dispose() => _harness.Dispose();

    private static async Task SettleAsync()
    {
        for (var i = 0; i < 5; i++)
        {
            Dispatcher.UIThread.RunJobs();
            await Task.Delay(20);
        }
    }

    [Fact]
    public Task ClickingTheBottomSearchResultAddsItsLineWithoutCrashing() => Headless.Run(async () =>
    {
        _harness.Catalog.Products.Add(new ProductSummary(Guid.NewGuid(), "P-1", "Cat6 cable", "Piece", 50, 25, true));
        _harness.Catalog.Products.Add(new ProductSummary(Guid.NewGuid(), "P-2", "Patch panel", "Piece", 300, 25, true));
        for (var i = 4; i <= 20; i++)
        {
            _harness.Catalog.Products.Add(new ProductSummary(Guid.NewGuid(), "P-" + i, "Product " + i, "Piece", 10, 25, true));
        }

        _harness.Catalog.Products.Add(new ProductSummary(Guid.NewGuid(), "Z-1", "Zzz silly test product", "Piece", 10, 25, true));
        var screen = _harness.OfferScreen();
        var window = new Window { Width = 1000, Height = 900, Content = new OfferScreenView { DataContext = screen } };
        window.Show();
        await screen.LoadCommand.ExecuteAsync(null);

        // An empty search lists every product.
        screen.ProductSearch = string.Empty;
        await screen.SearchProductsCommand.ExecuteAsync(null);
        await SettleAsync();
        var rows = window.GetVisualDescendants().OfType<ListBoxItem>().Where(i => i.DataContext is ProductSummary).ToList();
        var list = window.GetVisualDescendants().OfType<ListBox>().Single(l => l.ItemsSource == screen.ProductResults);
        list.GetVisualDescendants().OfType<ScrollViewer>().First().ScrollToEnd();
        await SettleAsync();
        rows = window.GetVisualDescendants().OfType<ListBoxItem>().Where(i => i.DataContext is ProductSummary).ToList();

        var point = rows[^1].TranslatePoint(new Point(10, 10), window)!.Value;
        window.MouseDown(point, MouseButton.Left);
        window.MouseUp(point, MouseButton.Left);
        await SettleAsync();

        screen.Lines.Single().Description.ShouldBe("Z-1 Zzz silly test product");
        screen.ProductResults.ShouldBeEmpty();
        screen.SelectedProduct.ShouldBeNull();
        window.Close();
    });
}

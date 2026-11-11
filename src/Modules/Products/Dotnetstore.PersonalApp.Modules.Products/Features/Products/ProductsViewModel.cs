using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dotnetstore.PersonalApp.Modules.Products.Domain;
using Dotnetstore.PersonalApp.Modules.Products.Features.ProductCategories;
using Dotnetstore.PersonalApp.Modules.Products.Features.Stock;
using Dotnetstore.PersonalApp.Modules.Products.Features.SupplierPrices;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;

namespace Dotnetstore.PersonalApp.Modules.Products.Features.Products;

internal sealed partial class ProductsViewModel(
    ListProducts.Handler list,
    CreateProduct.Handler create,
    UpdateProduct.Handler update,
    DeleteProduct.Handler delete,
    ListProductCategories.Handler categories,
    ILocalizationService localization,
    ProductPhotoPanelFactory? photoFactory = null,
    SupplierPricesViewModelFactory? pricesFactory = null,
    GetProductStock.Handler? stockHandler = null) : ViewModelBase
{
    /// <summary>Rows per page; keeps the table a fixed height however many products there are.</summary>
    private const int PageSize = 15;

    private bool _loading;
    private IReadOnlyList<ProductCategoryOption> _categoryOptions = [];

    public ObservableCollection<ProductRow> Products { get; } = [];

    /// <summary>The kind filter: an "all kinds" entry followed by every kind.</summary>
    public IReadOnlyList<ProductKindOption> FilterKinds { get; } = KindOptions(localization, includeAll: true);

    [ObservableProperty]
    public partial ProductKindOption? SelectedFilter { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(OpenDetailCommand))]
    [NotifyCanExecuteChangedFor(nameof(RequestDeleteCommand))]
    [NotifyCanExecuteChangedFor(nameof(ConfirmDeleteCommand))]
    public partial ProductRow? SelectedProduct { get; set; }

    [ObservableProperty]
    public partial ProductEditorViewModel? Editor { get; set; }

    [ObservableProperty]
    public partial ProductDetailViewModel? Detail { get; set; }

    [ObservableProperty]
    public partial string Search { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PageLabel))]
    [NotifyCanExecuteChangedFor(nameof(PreviousPageCommand))]
    [NotifyCanExecuteChangedFor(nameof(NextPageCommand))]
    public partial int CurrentPage { get; set; } = 1;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PageLabel))]
    [NotifyPropertyChangedFor(nameof(ShowPager))]
    [NotifyCanExecuteChangedFor(nameof(NextPageCommand))]
    public partial int PageCount { get; set; } = 1;

    public string PageLabel => localization.Format("Products_PageOf", CurrentPage, PageCount);

    public bool ShowPager => PageCount > 1;

    [ObservableProperty]
    public partial string? StatusMessage { get; set; }

    [ObservableProperty]
    public partial bool IsConfirmingDelete { get; set; }

    partial void OnSelectedFilterChanged(ProductKindOption? value)
    {
        if (!_loading)
        {
            CloseEditor();
            CurrentPage = 1;
            LoadCommand.Execute(null);
        }
    }

    partial void OnSelectedProductChanged(ProductRow? value)
    {
        IsConfirmingDelete = false;
        if (value is not null)
        {
            OpenEditor(value.Product);
        }
    }

    [RelayCommand]
    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        _loading = true;
        try
        {
            SelectedFilter ??= FilterKinds[0];
            await RefreshCategoriesAsync(cancellationToken);

            var response = await list.HandleAsync(new ListProducts.Query(Search, SelectedFilter.Kind, CurrentPage, PageSize), cancellationToken);

            // Deleting the last product on the last page leaves the current page empty: step back to the new last page.
            var pageCount = Math.Max(1, (response.TotalCount + PageSize - 1) / PageSize);
            if (CurrentPage > pageCount)
            {
                CurrentPage = pageCount;
                response = await list.HandleAsync(new ListProducts.Query(Search, SelectedFilter.Kind, CurrentPage, PageSize), cancellationToken);
            }

            PageCount = pageCount;

            Products.Clear();
            foreach (var product in response.Items)
            {
                Products.Add(ToRow(product));
            }

            StatusMessage = localization.Format("Products_Count", response.TotalCount);
        }
        finally
        {
            _loading = false;
        }
    }

    [RelayCommand]
    private async Task SearchAsync(CancellationToken cancellationToken)
    {
        CloseEditor();
        CurrentPage = 1;
        await LoadAsync(cancellationToken);
    }

    [RelayCommand]
    private void New()
    {
        SelectedProduct = null;
        OpenEditor(null);
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void OpenDetail()
    {
        if (SelectedProduct is not { } row)
        {
            return;
        }

        var product = row.Product;
        var detail = new ProductDetailViewModel(
            product,
            localization,
            photoFactory is null ? null : initials => photoFactory.Create(product.Id, initials),
            pricesFactory?.Create(product.Id, localization.Get($"ProductUnit_{product.Unit}")),
            stockHandler is not null && product.Kind != ProductKind.Consulting
                ? new ProductStockViewModel(product.Id, localization.Get($"ProductUnit_{product.Unit}"), stockHandler, localization)
                : null);
        detail.Closed += OnDetailClosed;
        detail.EditRequested += OnDetailEditRequested;
        Detail = detail;
    }

    private void OnDetailClosed(object? sender, EventArgs e) => CloseDetail();

    // Event handler, so it has to be async void; LoadAsync reports its own outcome on the status line.
    private async void OnDetailEditRequested(object? sender, EventArgs e)
    {
        var id = Detail?.Product.Id;
        CloseDetail();
        await LoadAsync(CancellationToken.None);
        SelectedProduct = Products.FirstOrDefault(r => r.Product.Id == id);
    }

    private void CloseDetail()
    {
        if (Detail is { } detail)
        {
            detail.Closed -= OnDetailClosed;
            detail.EditRequested -= OnDetailEditRequested;
        }

        Detail = null;

        // The photo may have changed, and the list shows its thumbnail.
        LoadCommand.Execute(null);
    }

    [RelayCommand(CanExecute = nameof(HasPreviousPage))]
    private async Task PreviousPageAsync(CancellationToken cancellationToken) => await GoToPageAsync(CurrentPage - 1, cancellationToken);

    [RelayCommand(CanExecute = nameof(HasNextPage))]
    private async Task NextPageAsync(CancellationToken cancellationToken) => await GoToPageAsync(CurrentPage + 1, cancellationToken);

    private bool HasPreviousPage() => CurrentPage > 1;

    private bool HasNextPage() => CurrentPage < PageCount;

    private async Task GoToPageAsync(int page, CancellationToken cancellationToken)
    {
        CloseEditor();
        CurrentPage = page;
        await LoadAsync(cancellationToken);
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void RequestDelete() => IsConfirmingDelete = true;

    [RelayCommand]
    private void CancelDelete() => IsConfirmingDelete = false;

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private async Task ConfirmDeleteAsync(CancellationToken cancellationToken)
    {
        if (SelectedProduct is not { } row)
        {
            return;
        }

        var result = await delete.HandleAsync(new DeleteProduct.Command(row.Product.Id), cancellationToken);

        CloseEditor();
        await LoadAsync(cancellationToken);
        if (!result.IsSuccess)
        {
            StatusMessage = localization.Describe(result.Error);
        }
    }

    private bool HasSelection() => SelectedProduct is not null;

    private static List<ProductKindOption> KindOptions(ILocalizationService localization, bool includeAll)
    {
        var options = Enum.GetValues<ProductKind>().Select(k => new ProductKindOption(k, localization.Get($"ProductKind_{k}"))).ToList();
        if (includeAll)
        {
            options.Insert(0, new ProductKindOption(null, localization.Get("Products_AllKinds")));
        }

        return options;
    }

    private ProductRow ToRow(ProductDto product) => new(
        product,
        localization.Get($"ProductKind_{product.Kind}"),
        string.Create(CultureInfo.CurrentCulture, $"{product.UnitPrice:N2} / {localization.Get($"ProductUnit_{product.Unit}")}"));

    private async Task RefreshCategoriesAsync(CancellationToken cancellationToken)
    {
        var response = await categories.HandleAsync(new ListProductCategories.Query(), cancellationToken);
        var options = new List<ProductCategoryOption> { new(null, localization.Get("Products_NoCategory")) };
        options.AddRange(response.Items.Select(c => new ProductCategoryOption(c.Id, c.Name)));
        _categoryOptions = options;
    }

    private void OpenEditor(ProductDto? existing)
    {
        var kinds = KindOptions(localization, includeAll: false);
        var units = Enum.GetValues<ProductUnit>().Select(u => new ProductUnitOption(u, localization.Get($"ProductUnit_{u}"))).ToList();

        DetachEditor();
        var editor = new ProductEditorViewModel(existing, kinds, units, _categoryOptions, create, update, localization);
        Editor = editor;
        editor.Saved += OnEditorSaved;
        editor.Cancelled += OnEditorCancelled;
    }

    private void CloseEditor()
    {
        DetachEditor();
        Editor = null;
        SelectedProduct = null;
        IsConfirmingDelete = false;
    }

    private void DetachEditor()
    {
        if (Editor is { } editor)
        {
            editor.Saved -= OnEditorSaved;
            editor.Cancelled -= OnEditorCancelled;
        }
    }

    // Event handler, so it has to be async void; LoadAsync reports its own outcome on the status line.
    private async void OnEditorSaved(object? sender, ProductDto saved)
    {
        CloseEditor();
        await LoadAsync(CancellationToken.None);
        StatusMessage = localization.Format("Products_Saved", saved.Name);
    }

    private void OnEditorCancelled(object? sender, EventArgs e) => CloseEditor();
}

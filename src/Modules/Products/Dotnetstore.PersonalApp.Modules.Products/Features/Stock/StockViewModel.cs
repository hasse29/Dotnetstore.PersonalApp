using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dotnetstore.PersonalApp.Modules.Products.Features.Suppliers;
using Dotnetstore.PersonalApp.Modules.Products.Features.StorageLocations;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;

namespace Dotnetstore.PersonalApp.Modules.Products.Features.Stock;

/// <summary>A product's stock as the list shows it: the DTO plus text in the current UI language.</summary>
internal sealed record StockRow(StockOverviewDto Item, string TotalText, string MinimumText, string StatusText);

internal sealed partial class StockViewModel(
    ListStockOverview.Handler overview,
    ListStorageLocations.Handler locations,
    ListSuppliers.Handler suppliers,
    ReceiveStock.Handler receive,
    IssueStock.Handler issue,
    TransferStock.Handler transfer,
    ILocalizationService localization) : ViewModelBase
{
    /// <summary>Rows per page; keeps the table a fixed height however many products there are.</summary>
    private const int PageSize = 15;

    public ObservableCollection<StockRow> Rows { get; } = [];

    [ObservableProperty]
    public partial StockRow? SelectedRow { get; set; }

    [ObservableProperty]
    public partial StockMovementEditorViewModel? Editor { get; set; }

    [ObservableProperty]
    public partial string Search { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool LowOnly { get; set; }

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

    public string PageLabel => localization.Format("Stock_PageOf", CurrentPage, PageCount);

    public bool ShowPager => PageCount > 1;

    [ObservableProperty]
    public partial string? StatusMessage { get; set; }

    partial void OnLowOnlyChanged(bool value)
    {
        CloseEditor();
        CurrentPage = 1;
        LoadCommand.Execute(null);
    }

    partial void OnSelectedRowChanged(StockRow? value)
    {
        if (value is not null)
        {
            OpenEditorCommand.Execute(value);
        }
    }

    [RelayCommand]
    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        var response = await overview.HandleAsync(new ListStockOverview.Query(Search, LowOnly, CurrentPage, PageSize), cancellationToken);

        // Fewer pages than before (e.g. after turning on "low only"): step back to the new last page.
        var pageCount = Math.Max(1, (response.TotalCount + PageSize - 1) / PageSize);
        if (CurrentPage > pageCount)
        {
            CurrentPage = pageCount;
            response = await overview.HandleAsync(new ListStockOverview.Query(Search, LowOnly, CurrentPage, PageSize), cancellationToken);
        }

        PageCount = pageCount;

        Rows.Clear();
        foreach (var item in response.Items)
        {
            Rows.Add(ToRow(item));
        }

        StatusMessage = localization.Format("Stock_Count", response.TotalCount);
    }

    [RelayCommand]
    private async Task SearchAsync(CancellationToken cancellationToken)
    {
        CloseEditor();
        CurrentPage = 1;
        await LoadAsync(cancellationToken);
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

    [RelayCommand]
    private async Task OpenEditorAsync(StockRow row, CancellationToken cancellationToken)
    {
        var modes = Enum.GetValues<StockMovementMode>()
            .Select(m => new StockMovementModeOption(m, localization.Get($"StockMode_{m}")))
            .ToList();

        var locationList = (await locations.HandleAsync(new ListStorageLocations.Query(), cancellationToken)).Items;
        var supplierList = (await suppliers.HandleAsync(new ListSuppliers.Query(ActiveOnly: true), cancellationToken)).Items;
        var supplierOptions = new List<SupplierOption> { new(null, localization.Get("StockEditor_NoSupplier")) };
        supplierOptions.AddRange(supplierList.Select(s => new SupplierOption(s.Id, s.Name)));

        DetachEditor();
        if (locationList.Count == 0)
        {
            Editor = null;
            StatusMessage = localization.Get("Stock_NoLocations");
            return;
        }

        var editor = new StockMovementEditorViewModel(
            row.Item, UnitLabelOf(row.Item), modes, locationList, supplierOptions, receive, issue, transfer, localization);
        Editor = editor;
        editor.Saved += OnEditorSaved;
        editor.Cancelled += OnEditorCancelled;
    }

    private void CloseEditor()
    {
        DetachEditor();
        Editor = null;
        SelectedRow = null;
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
    private async void OnEditorSaved(object? sender, string message)
    {
        CloseEditor();
        await LoadAsync(CancellationToken.None);
        StatusMessage = message;
    }

    private void OnEditorCancelled(object? sender, EventArgs e) => CloseEditor();

    private string UnitLabelOf(StockOverviewDto item) => localization.Get($"ProductUnit_{item.Unit}");

    private StockRow ToRow(StockOverviewDto item) => new(
        item,
        string.Create(CultureInfo.CurrentCulture, $"{item.Total:0.##} {UnitLabelOf(item)}"),
        item.MinimumStock is { } minimum ? string.Create(CultureInfo.CurrentCulture, $"{minimum:0.##}") : "-",
        item.IsLow ? localization.Get("Stock_Low") : string.Empty);
}

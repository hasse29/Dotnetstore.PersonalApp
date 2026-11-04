using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dotnetstore.PersonalApp.Modules.Inventory.Features.Warehouses;
using Dotnetstore.PersonalApp.Modules.Inventory.Features;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;

namespace Dotnetstore.PersonalApp.Modules.Inventory.Features.InventoryCounts;

internal sealed partial class InventoryCountsViewModel(
    ListInventoryCounts.Handler list,
    CreateInventoryCount.Handler create,
    UpdateInventoryCount.Handler update,
    DeleteInventoryCount.Handler delete,
    IDetailFactory<InventoryCountDto> detailFactory,
    ListWarehouseOptions.Handler listWarehouseOptions,
    TimeProvider clock,
    ILocalizationService localization) : ViewModelBase
{
    private IReadOnlyList<OptionDto> _warehouseOptions = [];

    private Guid? _parentId;

    /// <summary>True when the page sits in a detail page of its parent, so it drops its own title and margin.</summary>
    public bool IsEmbedded => _parentId is not null;

    /// <summary>Raised after a row was saved or deleted, so the page around it can refresh what it shows.</summary>
    public event EventHandler? Changed;

    /// <summary>Limits the list to one parent and gives new rows that parent; call before the first load.</summary>
    public InventoryCountsViewModel ForParent(Guid parentId)
    {
        _parentId = parentId;
        return this;
    }

    [ObservableProperty]
    public partial DetailViewModel? Detail { get; set; }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void OpenDetail()
    {
        if (SelectedInventoryCount is not { } selected)
        {
            return;
        }

        var detail = detailFactory.Create(selected);
        detail.Closed += OnDetailClosed;
        Detail = detail;
    }

    // Event handler, so it has to be async void; LoadAsync reports its own outcome on the status line.
    private async void OnDetailClosed(object? sender, EventArgs e)
    {
        if (Detail is { } detail)
        {
            detail.Closed -= OnDetailClosed;
        }

        Detail = null;
        CloseEditor();
        await LoadAsync(CancellationToken.None);
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public ObservableCollection<InventoryCountDto> InventoryCounts { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(OpenDetailCommand))]
    [NotifyCanExecuteChangedFor(nameof(RequestDeleteCommand))]
    [NotifyCanExecuteChangedFor(nameof(ConfirmDeleteCommand))]
    public partial InventoryCountDto? SelectedInventoryCount { get; set; }

    [ObservableProperty]
    public partial InventoryCountEditorViewModel? Editor { get; set; }

    [ObservableProperty]
    public partial string Search { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string? StatusMessage { get; set; }

    [ObservableProperty]
    public partial bool IsConfirmingDelete { get; set; }

    partial void OnSelectedInventoryCountChanged(InventoryCountDto? value)
    {
        IsConfirmingDelete = false;
        if (value is not null)
        {
            OpenEditor(new InventoryCountEditorViewModel(value, create, update, _warehouseOptions, clock, localization, _parentId));
        }
    }

    [RelayCommand]
    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        _warehouseOptions = await listWarehouseOptions.HandleAsync(cancellationToken);

        var response = await list.HandleAsync(new ListInventoryCounts.Query(Search, ParentId: _parentId), cancellationToken);

        InventoryCounts.Clear();
        foreach (var item in response.Items)
        {
            InventoryCounts.Add(item);
        }

        StatusMessage = response.TotalCount > response.Items.Count
            ? localization.Format("InventoryCounts_ShowingPartial", response.Items.Count, response.TotalCount)
            : localization.Format("InventoryCounts_Count", response.TotalCount);
    }

    [RelayCommand]
    private void New()
    {
        SelectedInventoryCount = null;
        OpenEditor(new InventoryCountEditorViewModel(null, create, update, _warehouseOptions, clock, localization, _parentId));
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void RequestDelete() => IsConfirmingDelete = true;

    [RelayCommand]
    private void CancelDelete() => IsConfirmingDelete = false;

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private async Task ConfirmDeleteAsync(CancellationToken cancellationToken)
    {
        if (SelectedInventoryCount is not { } selected)
        {
            return;
        }

        var result = await delete.HandleAsync(new DeleteInventoryCount.Command(selected.Id), cancellationToken);

        CloseEditor();
        await LoadAsync(cancellationToken);
        if (!result.IsSuccess)
        {
            StatusMessage = localization.Describe(result.Error);
        }
        else
        {
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }

    private bool HasSelection() => SelectedInventoryCount is not null;

    private void OpenEditor(InventoryCountEditorViewModel editor)
    {
        DetachEditor();
        Editor = editor;
        editor.Saved += OnEditorSaved;
        editor.Cancelled += OnEditorCancelled;
    }

    private void CloseEditor()
    {
        DetachEditor();
        Editor = null;
        SelectedInventoryCount = null;
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
    private async void OnEditorSaved(object? sender, InventoryCountDto saved)
    {
        CloseEditor();
        await LoadAsync(CancellationToken.None);
        StatusMessage = localization.Get("InventoryCounts_Saved");
        Changed?.Invoke(this, EventArgs.Empty);
    }

    private void OnEditorCancelled(object? sender, EventArgs e) => CloseEditor();
}
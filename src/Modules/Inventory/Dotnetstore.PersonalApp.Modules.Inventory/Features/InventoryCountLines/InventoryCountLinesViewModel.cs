using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dotnetstore.PersonalApp.Modules.Inventory.Features.InventoryCounts;
using Dotnetstore.PersonalApp.Modules.Inventory.Features.StorageLocations;
using Dotnetstore.PersonalApp.Modules.Inventory.Features;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;

namespace Dotnetstore.PersonalApp.Modules.Inventory.Features.InventoryCountLines;

internal sealed partial class InventoryCountLinesViewModel(
    ListInventoryCountLines.Handler list,
    CreateInventoryCountLine.Handler create,
    UpdateInventoryCountLine.Handler update,
    DeleteInventoryCountLine.Handler delete,
    ListInventoryCountOptions.Handler listInventoryCountOptions,
    ListStorageLocationOptions.Handler listStorageLocationOptions,
    ExternalReferences references,
    TimeProvider clock,
    ILocalizationService localization) : ViewModelBase
{
    private IReadOnlyList<OptionDto> _inventoryCountOptions = [];
    private IReadOnlyList<OptionDto> _storageLocationOptions = [];
    private IReadOnlyList<OptionDto> _productOptions = [];

    private Guid? _parentId;

    /// <summary>True when the page sits in a detail page of its parent, so it drops its own title and margin.</summary>
    public bool IsEmbedded => _parentId is not null;

    /// <summary>Raised after a row was saved or deleted, so the page around it can refresh what it shows.</summary>
    public event EventHandler? Changed;

    /// <summary>Limits the list to one parent and gives new rows that parent; call before the first load.</summary>
    public InventoryCountLinesViewModel ForParent(Guid parentId)
    {
        _parentId = parentId;
        return this;
    }

    public ObservableCollection<InventoryCountLineDto> InventoryCountLines { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RequestDeleteCommand))]
    [NotifyCanExecuteChangedFor(nameof(ConfirmDeleteCommand))]
    public partial InventoryCountLineDto? SelectedInventoryCountLine { get; set; }

    [ObservableProperty]
    public partial InventoryCountLineEditorViewModel? Editor { get; set; }

    [ObservableProperty]
    public partial string Search { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string? StatusMessage { get; set; }

    [ObservableProperty]
    public partial bool IsConfirmingDelete { get; set; }

    partial void OnSelectedInventoryCountLineChanged(InventoryCountLineDto? value)
    {
        IsConfirmingDelete = false;
        if (value is not null)
        {
            OpenEditor(new InventoryCountLineEditorViewModel(value, create, update, _inventoryCountOptions, _storageLocationOptions, _productOptions, clock, localization, _parentId));
        }
    }

    [RelayCommand]
    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        _inventoryCountOptions = await listInventoryCountOptions.HandleAsync(cancellationToken);
        _storageLocationOptions = await listStorageLocationOptions.HandleAsync(cancellationToken);
        _productOptions = await references.ProductOptionsAsync(cancellationToken);

        var response = await list.HandleAsync(new ListInventoryCountLines.Query(Search, ParentId: _parentId), cancellationToken);

        InventoryCountLines.Clear();
        foreach (var item in response.Items)
        {
            InventoryCountLines.Add(item);
        }

        StatusMessage = response.TotalCount > response.Items.Count
            ? localization.Format("InventoryCountLines_ShowingPartial", response.Items.Count, response.TotalCount)
            : localization.Format("InventoryCountLines_Count", response.TotalCount);
    }

    [RelayCommand]
    private void New()
    {
        SelectedInventoryCountLine = null;
        OpenEditor(new InventoryCountLineEditorViewModel(null, create, update, _inventoryCountOptions, _storageLocationOptions, _productOptions, clock, localization, _parentId));
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void RequestDelete() => IsConfirmingDelete = true;

    [RelayCommand]
    private void CancelDelete() => IsConfirmingDelete = false;

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private async Task ConfirmDeleteAsync(CancellationToken cancellationToken)
    {
        if (SelectedInventoryCountLine is not { } selected)
        {
            return;
        }

        var result = await delete.HandleAsync(new DeleteInventoryCountLine.Command(selected.Id), cancellationToken);

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

    private bool HasSelection() => SelectedInventoryCountLine is not null;

    private void OpenEditor(InventoryCountLineEditorViewModel editor)
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
        SelectedInventoryCountLine = null;
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
    private async void OnEditorSaved(object? sender, InventoryCountLineDto saved)
    {
        CloseEditor();
        await LoadAsync(CancellationToken.None);
        StatusMessage = localization.Get("InventoryCountLines_Saved");
        Changed?.Invoke(this, EventArgs.Empty);
    }

    private void OnEditorCancelled(object? sender, EventArgs e) => CloseEditor();
}
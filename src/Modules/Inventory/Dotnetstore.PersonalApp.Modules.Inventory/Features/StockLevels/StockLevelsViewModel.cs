using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dotnetstore.PersonalApp.Modules.Inventory.Features.StorageLocations;
using Dotnetstore.PersonalApp.Modules.Inventory.Features;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;

namespace Dotnetstore.PersonalApp.Modules.Inventory.Features.StockLevels;

internal sealed partial class StockLevelsViewModel(
    ListStockLevels.Handler list,
    CreateStockLevel.Handler create,
    UpdateStockLevel.Handler update,
    DeleteStockLevel.Handler delete,
    ListStorageLocationOptions.Handler listStorageLocationOptions,
    ExternalReferences references,
    TimeProvider clock,
    ILocalizationService localization) : ViewModelBase
{
    private IReadOnlyList<OptionDto> _storageLocationOptions = [];
    private IReadOnlyList<OptionDto> _productOptions = [];

    private Guid? _parentId;

    /// <summary>True when the page sits in a detail page of its parent, so it drops its own title and margin.</summary>
    public bool IsEmbedded => _parentId is not null;

    /// <summary>Raised after a row was saved or deleted, so the page around it can refresh what it shows.</summary>
    public event EventHandler? Changed;

    /// <summary>Limits the list to one parent and gives new rows that parent; call before the first load.</summary>
    public StockLevelsViewModel ForParent(Guid parentId)
    {
        _parentId = parentId;
        return this;
    }

    public ObservableCollection<StockLevelDto> StockLevels { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RequestDeleteCommand))]
    [NotifyCanExecuteChangedFor(nameof(ConfirmDeleteCommand))]
    public partial StockLevelDto? SelectedStockLevel { get; set; }

    [ObservableProperty]
    public partial StockLevelEditorViewModel? Editor { get; set; }

    [ObservableProperty]
    public partial string Search { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string? StatusMessage { get; set; }

    [ObservableProperty]
    public partial bool IsConfirmingDelete { get; set; }

    partial void OnSelectedStockLevelChanged(StockLevelDto? value)
    {
        IsConfirmingDelete = false;
        if (value is not null)
        {
            OpenEditor(new StockLevelEditorViewModel(value, create, update, _storageLocationOptions, _productOptions, clock, localization, _parentId));
        }
    }

    [RelayCommand]
    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        _storageLocationOptions = await listStorageLocationOptions.HandleAsync(cancellationToken);
        _productOptions = await references.ProductOptionsAsync(cancellationToken);

        var response = await list.HandleAsync(new ListStockLevels.Query(Search, ParentId: _parentId), cancellationToken);

        StockLevels.Clear();
        foreach (var item in response.Items)
        {
            StockLevels.Add(item);
        }

        StatusMessage = response.TotalCount > response.Items.Count
            ? localization.Format("StockLevels_ShowingPartial", response.Items.Count, response.TotalCount)
            : localization.Format("StockLevels_Count", response.TotalCount);
    }

    [RelayCommand]
    private void New()
    {
        SelectedStockLevel = null;
        OpenEditor(new StockLevelEditorViewModel(null, create, update, _storageLocationOptions, _productOptions, clock, localization, _parentId));
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void RequestDelete() => IsConfirmingDelete = true;

    [RelayCommand]
    private void CancelDelete() => IsConfirmingDelete = false;

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private async Task ConfirmDeleteAsync(CancellationToken cancellationToken)
    {
        if (SelectedStockLevel is not { } selected)
        {
            return;
        }

        var result = await delete.HandleAsync(new DeleteStockLevel.Command(selected.Id), cancellationToken);

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

    private bool HasSelection() => SelectedStockLevel is not null;

    private void OpenEditor(StockLevelEditorViewModel editor)
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
        SelectedStockLevel = null;
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
    private async void OnEditorSaved(object? sender, StockLevelDto saved)
    {
        CloseEditor();
        await LoadAsync(CancellationToken.None);
        StatusMessage = localization.Get("StockLevels_Saved");
        Changed?.Invoke(this, EventArgs.Empty);
    }

    private void OnEditorCancelled(object? sender, EventArgs e) => CloseEditor();
}
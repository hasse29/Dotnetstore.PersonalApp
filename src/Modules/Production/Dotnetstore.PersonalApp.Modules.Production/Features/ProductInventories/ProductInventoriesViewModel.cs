using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dotnetstore.PersonalApp.Modules.Production.Features.Locations;
using Dotnetstore.PersonalApp.Modules.Production.Features.Products;
using Dotnetstore.PersonalApp.Modules.Production.Features;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;

namespace Dotnetstore.PersonalApp.Modules.Production.Features.ProductInventories;

internal sealed partial class ProductInventoriesViewModel(
    ListProductInventories.Handler list,
    CreateProductInventory.Handler create,
    UpdateProductInventory.Handler update,
    DeleteProductInventory.Handler delete,
    ListProductOptions.Handler listProductOptions,
    ListLocationOptions.Handler listLocationOptions,
    TimeProvider clock,
    ILocalizationService localization) : ViewModelBase
{
    private IReadOnlyList<OptionDto> _productOptions = [];
    private IReadOnlyList<OptionDto> _locationOptions = [];

    private Guid? _parentId;

    /// <summary>True when the page sits in a detail page of its parent, so it drops its own title and margin.</summary>
    public bool IsEmbedded => _parentId is not null;

    /// <summary>Raised after a row was saved or deleted, so the page around it can refresh what it shows.</summary>
    public event EventHandler? Changed;

    /// <summary>Limits the list to one parent and gives new rows that parent; call before the first load.</summary>
    public ProductInventoriesViewModel ForParent(Guid parentId)
    {
        _parentId = parentId;
        return this;
    }

    public ObservableCollection<ProductInventoryDto> ProductInventories { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RequestDeleteCommand))]
    [NotifyCanExecuteChangedFor(nameof(ConfirmDeleteCommand))]
    public partial ProductInventoryDto? SelectedProductInventory { get; set; }

    [ObservableProperty]
    public partial ProductInventoryEditorViewModel? Editor { get; set; }

    [ObservableProperty]
    public partial string Search { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string? StatusMessage { get; set; }

    [ObservableProperty]
    public partial bool IsConfirmingDelete { get; set; }

    partial void OnSelectedProductInventoryChanged(ProductInventoryDto? value)
    {
        IsConfirmingDelete = false;
        if (value is not null)
        {
            OpenEditor(new ProductInventoryEditorViewModel(value, create, update, _productOptions, _locationOptions, clock, localization, _parentId));
        }
    }

    [RelayCommand]
    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        _productOptions = await listProductOptions.HandleAsync(cancellationToken);
        _locationOptions = await listLocationOptions.HandleAsync(cancellationToken);

        var response = await list.HandleAsync(new ListProductInventories.Query(Search, ParentId: _parentId), cancellationToken);

        ProductInventories.Clear();
        foreach (var item in response.Items)
        {
            ProductInventories.Add(item);
        }

        StatusMessage = response.TotalCount > response.Items.Count
            ? localization.Format("ProductInventories_ShowingPartial", response.Items.Count, response.TotalCount)
            : localization.Format("ProductInventories_Count", response.TotalCount);
    }

    [RelayCommand]
    private void New()
    {
        SelectedProductInventory = null;
        OpenEditor(new ProductInventoryEditorViewModel(null, create, update, _productOptions, _locationOptions, clock, localization, _parentId));
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void RequestDelete() => IsConfirmingDelete = true;

    [RelayCommand]
    private void CancelDelete() => IsConfirmingDelete = false;

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private async Task ConfirmDeleteAsync(CancellationToken cancellationToken)
    {
        if (SelectedProductInventory is not { } selected)
        {
            return;
        }

        var result = await delete.HandleAsync(new DeleteProductInventory.Command(selected.Id), cancellationToken);

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

    private bool HasSelection() => SelectedProductInventory is not null;

    private void OpenEditor(ProductInventoryEditorViewModel editor)
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
        SelectedProductInventory = null;
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
    private async void OnEditorSaved(object? sender, ProductInventoryDto saved)
    {
        CloseEditor();
        await LoadAsync(CancellationToken.None);
        StatusMessage = localization.Get("ProductInventories_Saved");
        Changed?.Invoke(this, EventArgs.Empty);
    }

    private void OnEditorCancelled(object? sender, EventArgs e) => CloseEditor();
}
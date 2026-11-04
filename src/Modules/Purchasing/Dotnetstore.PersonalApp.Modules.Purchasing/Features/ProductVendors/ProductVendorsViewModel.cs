using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dotnetstore.PersonalApp.Modules.Purchasing.Features.Vendors;
using Dotnetstore.PersonalApp.Modules.Purchasing.Features;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;

namespace Dotnetstore.PersonalApp.Modules.Purchasing.Features.ProductVendors;

internal sealed partial class ProductVendorsViewModel(
    ListProductVendors.Handler list,
    CreateProductVendor.Handler create,
    UpdateProductVendor.Handler update,
    DeleteProductVendor.Handler delete,
    ListVendorOptions.Handler listVendorOptions,
    ExternalReferences references,
    TimeProvider clock,
    ILocalizationService localization) : ViewModelBase
{
    private IReadOnlyList<OptionDto> _vendorOptions = [];
    private IReadOnlyList<OptionDto> _productOptions = [];
    private IReadOnlyList<OptionDto> _unitMeasureOptions = [];

    private Guid? _parentId;

    /// <summary>True when the page sits in a detail page of its parent, so it drops its own title and margin.</summary>
    public bool IsEmbedded => _parentId is not null;

    /// <summary>Raised after a row was saved or deleted, so the page around it can refresh what it shows.</summary>
    public event EventHandler? Changed;

    /// <summary>Limits the list to one parent and gives new rows that parent; call before the first load.</summary>
    public ProductVendorsViewModel ForParent(Guid parentId)
    {
        _parentId = parentId;
        return this;
    }

    public ObservableCollection<ProductVendorDto> ProductVendors { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RequestDeleteCommand))]
    [NotifyCanExecuteChangedFor(nameof(ConfirmDeleteCommand))]
    public partial ProductVendorDto? SelectedProductVendor { get; set; }

    [ObservableProperty]
    public partial ProductVendorEditorViewModel? Editor { get; set; }

    [ObservableProperty]
    public partial string Search { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string? StatusMessage { get; set; }

    [ObservableProperty]
    public partial bool IsConfirmingDelete { get; set; }

    partial void OnSelectedProductVendorChanged(ProductVendorDto? value)
    {
        IsConfirmingDelete = false;
        if (value is not null)
        {
            OpenEditor(new ProductVendorEditorViewModel(value, create, update, _vendorOptions, _productOptions, _unitMeasureOptions, clock, localization, _parentId));
        }
    }

    [RelayCommand]
    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        _vendorOptions = await listVendorOptions.HandleAsync(cancellationToken);
        _productOptions = await references.ProductOptionsAsync(cancellationToken);
        _unitMeasureOptions = await references.UnitMeasureOptionsAsync(cancellationToken);

        var response = await list.HandleAsync(new ListProductVendors.Query(Search, ParentId: _parentId), cancellationToken);

        ProductVendors.Clear();
        foreach (var item in response.Items)
        {
            ProductVendors.Add(item);
        }

        StatusMessage = response.TotalCount > response.Items.Count
            ? localization.Format("ProductVendors_ShowingPartial", response.Items.Count, response.TotalCount)
            : localization.Format("ProductVendors_Count", response.TotalCount);
    }

    [RelayCommand]
    private void New()
    {
        SelectedProductVendor = null;
        OpenEditor(new ProductVendorEditorViewModel(null, create, update, _vendorOptions, _productOptions, _unitMeasureOptions, clock, localization, _parentId));
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void RequestDelete() => IsConfirmingDelete = true;

    [RelayCommand]
    private void CancelDelete() => IsConfirmingDelete = false;

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private async Task ConfirmDeleteAsync(CancellationToken cancellationToken)
    {
        if (SelectedProductVendor is not { } selected)
        {
            return;
        }

        var result = await delete.HandleAsync(new DeleteProductVendor.Command(selected.Id), cancellationToken);

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

    private bool HasSelection() => SelectedProductVendor is not null;

    private void OpenEditor(ProductVendorEditorViewModel editor)
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
        SelectedProductVendor = null;
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
    private async void OnEditorSaved(object? sender, ProductVendorDto saved)
    {
        CloseEditor();
        await LoadAsync(CancellationToken.None);
        StatusMessage = localization.Get("ProductVendors_Saved");
        Changed?.Invoke(this, EventArgs.Empty);
    }

    private void OnEditorCancelled(object? sender, EventArgs e) => CloseEditor();
}
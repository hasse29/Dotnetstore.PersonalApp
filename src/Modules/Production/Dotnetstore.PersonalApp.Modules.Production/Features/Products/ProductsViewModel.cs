using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dotnetstore.PersonalApp.Modules.Production.Features.ProductModels;
using Dotnetstore.PersonalApp.Modules.Production.Features.ProductSubcategories;
using Dotnetstore.PersonalApp.Modules.Production.Features.UnitMeasures;
using Dotnetstore.PersonalApp.Modules.Production.Features;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;

namespace Dotnetstore.PersonalApp.Modules.Production.Features.Products;

internal sealed partial class ProductsViewModel(
    ListProducts.Handler list,
    CreateProduct.Handler create,
    UpdateProduct.Handler update,
    DeleteProduct.Handler delete,
    IDetailFactory<ProductDto> detailFactory,
    ListUnitMeasureOptions.Handler listUnitMeasureOptions,
    ListProductSubcategoryOptions.Handler listProductSubcategoryOptions,
    ListProductModelOptions.Handler listProductModelOptions,
    TimeProvider clock,
    ILocalizationService localization) : ViewModelBase
{
    private IReadOnlyList<OptionDto> _unitMeasureOptions = [];
    private IReadOnlyList<OptionDto> _productSubcategoryOptions = [];
    private IReadOnlyList<OptionDto> _productModelOptions = [];

    [ObservableProperty]
    public partial DetailViewModel? Detail { get; set; }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void OpenDetail()
    {
        if (SelectedProduct is not { } selected)
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
    }

    public ObservableCollection<ProductDto> Products { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(OpenDetailCommand))]
    [NotifyCanExecuteChangedFor(nameof(RequestDeleteCommand))]
    [NotifyCanExecuteChangedFor(nameof(ConfirmDeleteCommand))]
    public partial ProductDto? SelectedProduct { get; set; }

    [ObservableProperty]
    public partial ProductEditorViewModel? Editor { get; set; }

    [ObservableProperty]
    public partial string Search { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string? StatusMessage { get; set; }

    [ObservableProperty]
    public partial bool IsConfirmingDelete { get; set; }

    partial void OnSelectedProductChanged(ProductDto? value)
    {
        IsConfirmingDelete = false;
        if (value is not null)
        {
            OpenEditor(new ProductEditorViewModel(value, create, update, _unitMeasureOptions, _productSubcategoryOptions, _productModelOptions, clock, localization));
        }
    }

    [RelayCommand]
    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        _unitMeasureOptions = await listUnitMeasureOptions.HandleAsync(cancellationToken);
        _productSubcategoryOptions = await listProductSubcategoryOptions.HandleAsync(cancellationToken);
        _productModelOptions = await listProductModelOptions.HandleAsync(cancellationToken);

        var response = await list.HandleAsync(new ListProducts.Query(Search), cancellationToken);

        Products.Clear();
        foreach (var item in response.Items)
        {
            Products.Add(item);
        }

        StatusMessage = response.TotalCount > response.Items.Count
            ? localization.Format("Products_ShowingPartial", response.Items.Count, response.TotalCount)
            : localization.Format("Products_Count", response.TotalCount);
    }

    [RelayCommand]
    private void New()
    {
        SelectedProduct = null;
        OpenEditor(new ProductEditorViewModel(null, create, update, _unitMeasureOptions, _productSubcategoryOptions, _productModelOptions, clock, localization));
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void RequestDelete() => IsConfirmingDelete = true;

    [RelayCommand]
    private void CancelDelete() => IsConfirmingDelete = false;

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private async Task ConfirmDeleteAsync(CancellationToken cancellationToken)
    {
        if (SelectedProduct is not { } selected)
        {
            return;
        }

        var result = await delete.HandleAsync(new DeleteProduct.Command(selected.Id), cancellationToken);

        CloseEditor();
        await LoadAsync(cancellationToken);
        if (!result.IsSuccess)
        {
            StatusMessage = localization.Describe(result.Error);
        }
    }

    private bool HasSelection() => SelectedProduct is not null;

    private void OpenEditor(ProductEditorViewModel editor)
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
        StatusMessage = localization.Get("Products_Saved");
    }

    private void OnEditorCancelled(object? sender, EventArgs e) => CloseEditor();
}
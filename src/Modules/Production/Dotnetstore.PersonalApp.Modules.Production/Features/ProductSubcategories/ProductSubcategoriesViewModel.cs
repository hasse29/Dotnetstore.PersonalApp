using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dotnetstore.PersonalApp.Modules.Production.Features.ProductCategories;
using Dotnetstore.PersonalApp.Modules.Production.Features;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;

namespace Dotnetstore.PersonalApp.Modules.Production.Features.ProductSubcategories;

internal sealed partial class ProductSubcategoriesViewModel(
    ListProductSubcategories.Handler list,
    CreateProductSubcategory.Handler create,
    UpdateProductSubcategory.Handler update,
    DeleteProductSubcategory.Handler delete,
    ListProductCategoryOptions.Handler listProductCategoryOptions,
    TimeProvider clock,
    ILocalizationService localization) : ViewModelBase
{
    private IReadOnlyList<OptionDto> _productCategoryOptions = [];

    public ObservableCollection<ProductSubcategoryDto> ProductSubcategories { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RequestDeleteCommand))]
    [NotifyCanExecuteChangedFor(nameof(ConfirmDeleteCommand))]
    public partial ProductSubcategoryDto? SelectedProductSubcategory { get; set; }

    [ObservableProperty]
    public partial ProductSubcategoryEditorViewModel? Editor { get; set; }

    [ObservableProperty]
    public partial string Search { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string? StatusMessage { get; set; }

    [ObservableProperty]
    public partial bool IsConfirmingDelete { get; set; }

    partial void OnSelectedProductSubcategoryChanged(ProductSubcategoryDto? value)
    {
        IsConfirmingDelete = false;
        if (value is not null)
        {
            OpenEditor(new ProductSubcategoryEditorViewModel(value, create, update, _productCategoryOptions, clock, localization));
        }
    }

    [RelayCommand]
    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        _productCategoryOptions = await listProductCategoryOptions.HandleAsync(cancellationToken);

        var response = await list.HandleAsync(new ListProductSubcategories.Query(Search), cancellationToken);

        ProductSubcategories.Clear();
        foreach (var item in response.Items)
        {
            ProductSubcategories.Add(item);
        }

        StatusMessage = response.TotalCount > response.Items.Count
            ? localization.Format("ProductSubcategories_ShowingPartial", response.Items.Count, response.TotalCount)
            : localization.Format("ProductSubcategories_Count", response.TotalCount);
    }

    [RelayCommand]
    private void New()
    {
        SelectedProductSubcategory = null;
        OpenEditor(new ProductSubcategoryEditorViewModel(null, create, update, _productCategoryOptions, clock, localization));
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void RequestDelete() => IsConfirmingDelete = true;

    [RelayCommand]
    private void CancelDelete() => IsConfirmingDelete = false;

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private async Task ConfirmDeleteAsync(CancellationToken cancellationToken)
    {
        if (SelectedProductSubcategory is not { } selected)
        {
            return;
        }

        var result = await delete.HandleAsync(new DeleteProductSubcategory.Command(selected.Id), cancellationToken);

        CloseEditor();
        await LoadAsync(cancellationToken);
        if (!result.IsSuccess)
        {
            StatusMessage = localization.Describe(result.Error);
        }
    }

    private bool HasSelection() => SelectedProductSubcategory is not null;

    private void OpenEditor(ProductSubcategoryEditorViewModel editor)
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
        SelectedProductSubcategory = null;
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
    private async void OnEditorSaved(object? sender, ProductSubcategoryDto saved)
    {
        CloseEditor();
        await LoadAsync(CancellationToken.None);
        StatusMessage = localization.Get("ProductSubcategories_Saved");
    }

    private void OnEditorCancelled(object? sender, EventArgs e) => CloseEditor();
}
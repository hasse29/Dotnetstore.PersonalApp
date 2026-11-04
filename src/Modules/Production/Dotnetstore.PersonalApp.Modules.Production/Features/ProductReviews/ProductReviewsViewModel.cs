using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dotnetstore.PersonalApp.Modules.Production.Features.Products;
using Dotnetstore.PersonalApp.Modules.Production.Features;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;

namespace Dotnetstore.PersonalApp.Modules.Production.Features.ProductReviews;

internal sealed partial class ProductReviewsViewModel(
    ListProductReviews.Handler list,
    CreateProductReview.Handler create,
    UpdateProductReview.Handler update,
    DeleteProductReview.Handler delete,
    ListProductOptions.Handler listProductOptions,
    TimeProvider clock,
    ILocalizationService localization) : ViewModelBase
{
    private IReadOnlyList<OptionDto> _productOptions = [];

    private Guid? _parentId;

    /// <summary>True when the page sits in a detail page of its parent, so it drops its own title and margin.</summary>
    public bool IsEmbedded => _parentId is not null;

    /// <summary>Raised after a row was saved or deleted, so the page around it can refresh what it shows.</summary>
    public event EventHandler? Changed;

    /// <summary>Limits the list to one parent and gives new rows that parent; call before the first load.</summary>
    public ProductReviewsViewModel ForParent(Guid parentId)
    {
        _parentId = parentId;
        return this;
    }

    public ObservableCollection<ProductReviewDto> ProductReviews { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RequestDeleteCommand))]
    [NotifyCanExecuteChangedFor(nameof(ConfirmDeleteCommand))]
    public partial ProductReviewDto? SelectedProductReview { get; set; }

    [ObservableProperty]
    public partial ProductReviewEditorViewModel? Editor { get; set; }

    [ObservableProperty]
    public partial string Search { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string? StatusMessage { get; set; }

    [ObservableProperty]
    public partial bool IsConfirmingDelete { get; set; }

    partial void OnSelectedProductReviewChanged(ProductReviewDto? value)
    {
        IsConfirmingDelete = false;
        if (value is not null)
        {
            OpenEditor(new ProductReviewEditorViewModel(value, create, update, _productOptions, clock, localization, _parentId));
        }
    }

    [RelayCommand]
    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        _productOptions = await listProductOptions.HandleAsync(cancellationToken);

        var response = await list.HandleAsync(new ListProductReviews.Query(Search, ParentId: _parentId), cancellationToken);

        ProductReviews.Clear();
        foreach (var item in response.Items)
        {
            ProductReviews.Add(item);
        }

        StatusMessage = response.TotalCount > response.Items.Count
            ? localization.Format("ProductReviews_ShowingPartial", response.Items.Count, response.TotalCount)
            : localization.Format("ProductReviews_Count", response.TotalCount);
    }

    [RelayCommand]
    private void New()
    {
        SelectedProductReview = null;
        OpenEditor(new ProductReviewEditorViewModel(null, create, update, _productOptions, clock, localization, _parentId));
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void RequestDelete() => IsConfirmingDelete = true;

    [RelayCommand]
    private void CancelDelete() => IsConfirmingDelete = false;

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private async Task ConfirmDeleteAsync(CancellationToken cancellationToken)
    {
        if (SelectedProductReview is not { } selected)
        {
            return;
        }

        var result = await delete.HandleAsync(new DeleteProductReview.Command(selected.Id), cancellationToken);

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

    private bool HasSelection() => SelectedProductReview is not null;

    private void OpenEditor(ProductReviewEditorViewModel editor)
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
        SelectedProductReview = null;
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
    private async void OnEditorSaved(object? sender, ProductReviewDto saved)
    {
        CloseEditor();
        await LoadAsync(CancellationToken.None);
        StatusMessage = localization.Get("ProductReviews_Saved");
        Changed?.Invoke(this, EventArgs.Empty);
    }

    private void OnEditorCancelled(object? sender, EventArgs e) => CloseEditor();
}
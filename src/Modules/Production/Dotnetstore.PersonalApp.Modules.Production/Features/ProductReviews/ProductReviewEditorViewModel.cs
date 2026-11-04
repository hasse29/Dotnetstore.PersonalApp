using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dotnetstore.PersonalApp.Modules.Production.Features;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;

namespace Dotnetstore.PersonalApp.Modules.Production.Features.ProductReviews;

internal sealed partial class ProductReviewEditorViewModel : ViewModelBase
{
    private readonly ProductReviewDto? _existing;
    private readonly CreateProductReview.Handler _create;
    private readonly UpdateProductReview.Handler _update;
    private readonly ILocalizationService _localization;

    public ProductReviewEditorViewModel(
        ProductReviewDto? existing,
        CreateProductReview.Handler create,
        UpdateProductReview.Handler update,
        IReadOnlyList<OptionDto> productOptions,
        TimeProvider clock,
        ILocalizationService localization,
        Guid? fixedParentId = null)
    {
        _existing = existing;
        _create = create;
        _update = update;
        _localization = localization;

        ProductOptions = productOptions;

        if (existing is not null)
        {
            SelectedProduct = ProductOptions.FirstOrDefault(o => o.Id == existing.ProductId);
            ReviewerName = existing.ReviewerName;
            ReviewDate = existing.ReviewDate.ToDateTime(TimeOnly.MinValue);
            EmailAddress = existing.EmailAddress;
            Rating = existing.Rating;
            Comments = existing.Comments ?? string.Empty;
        }
        else
        {
            ReviewDate = clock.GetLocalNow().Date;
        }

        if (fixedParentId is { } parentId)
        {
            SelectedProduct = ProductOptions.FirstOrDefault(o => o.Id == parentId) ?? SelectedProduct;
            IsParentFixed = true;
        }
    }

    public event EventHandler<ProductReviewDto>? Saved;

    public event EventHandler? Cancelled;

    public string Title => _localization.Get(_existing is null ? "ProductReviewEditor_New" : "ProductReviewEditor_Edit");

    public bool IsParentFixed { get; }

    public IReadOnlyList<OptionDto> ProductOptions { get; }

    [ObservableProperty]
    public partial OptionDto? SelectedProduct { get; set; }

    [ObservableProperty]
    public partial string ReviewerName { get; set; } = string.Empty;

    /// <summary>A <see cref="DateTime"/> because that is what the date picker binds to.</summary>
    [ObservableProperty]
    public partial DateTime? ReviewDate { get; set; }

    [ObservableProperty]
    public partial string EmailAddress { get; set; } = string.Empty;

    [ObservableProperty]
    public partial decimal? Rating { get; set; }

    [ObservableProperty]
    public partial string Comments { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    [RelayCommand]
    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        ErrorMessage = null;

        var result = _existing is not { } existing
            ? await _create.HandleAsync(new CreateProductReview.Command(SelectedProduct?.Id ?? Guid.Empty, ReviewerName, DateOnly.FromDateTime(ReviewDate ?? DateTime.MinValue), EmailAddress, (int)(Rating ?? 0), Comments), cancellationToken)
            : await _update.HandleAsync(new UpdateProductReview.Command(existing.Id, SelectedProduct?.Id ?? Guid.Empty, ReviewerName, DateOnly.FromDateTime(ReviewDate ?? DateTime.MinValue), EmailAddress, (int)(Rating ?? 0), Comments), cancellationToken);

        if (result.IsSuccess)
        {
            Saved?.Invoke(this, result.Value);
        }
        else
        {
            ErrorMessage = _localization.Describe(result.Error);
        }
    }

    [RelayCommand]
    private void Cancel() => Cancelled?.Invoke(this, EventArgs.Empty);

}
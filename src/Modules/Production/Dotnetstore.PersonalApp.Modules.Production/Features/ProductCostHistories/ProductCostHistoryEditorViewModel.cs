using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dotnetstore.PersonalApp.Modules.Production.Features;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;

namespace Dotnetstore.PersonalApp.Modules.Production.Features.ProductCostHistories;

internal sealed partial class ProductCostHistoryEditorViewModel : ViewModelBase
{
    private readonly ProductCostHistoryDto? _existing;
    private readonly CreateProductCostHistory.Handler _create;
    private readonly UpdateProductCostHistory.Handler _update;
    private readonly ILocalizationService _localization;

    public ProductCostHistoryEditorViewModel(
        ProductCostHistoryDto? existing,
        CreateProductCostHistory.Handler create,
        UpdateProductCostHistory.Handler update,
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
            StartDate = existing.StartDate.ToDateTime(TimeOnly.MinValue);
            EndDate = existing.EndDate?.ToDateTime(TimeOnly.MinValue);
            StandardCost = existing.StandardCost;
        }
        else
        {
            StartDate = clock.GetLocalNow().Date;
        }

        if (fixedParentId is { } parentId)
        {
            SelectedProduct = ProductOptions.FirstOrDefault(o => o.Id == parentId) ?? SelectedProduct;
            IsParentFixed = true;
        }
    }

    public event EventHandler<ProductCostHistoryDto>? Saved;

    public event EventHandler? Cancelled;

    public string Title => _localization.Get(_existing is null ? "ProductCostHistoryEditor_New" : "ProductCostHistoryEditor_Edit");

    public bool IsParentFixed { get; }

    public IReadOnlyList<OptionDto> ProductOptions { get; }

    [ObservableProperty]
    public partial OptionDto? SelectedProduct { get; set; }

    /// <summary>A <see cref="DateTime"/> because that is what the date picker binds to.</summary>
    [ObservableProperty]
    public partial DateTime? StartDate { get; set; }

    /// <summary>A <see cref="DateTime"/> because that is what the date picker binds to.</summary>
    [ObservableProperty]
    public partial DateTime? EndDate { get; set; }

    [ObservableProperty]
    public partial decimal? StandardCost { get; set; }

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    [RelayCommand]
    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        ErrorMessage = null;

        var result = _existing is not { } existing
            ? await _create.HandleAsync(new CreateProductCostHistory.Command(SelectedProduct?.Id ?? Guid.Empty, DateOnly.FromDateTime(StartDate ?? DateTime.MinValue), ToDate(EndDate), StandardCost ?? 0m), cancellationToken)
            : await _update.HandleAsync(new UpdateProductCostHistory.Command(existing.Id, SelectedProduct?.Id ?? Guid.Empty, DateOnly.FromDateTime(StartDate ?? DateTime.MinValue), ToDate(EndDate), StandardCost ?? 0m), cancellationToken);

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

    private static DateOnly? ToDate(DateTime? value) => value is { } v ? DateOnly.FromDateTime(v) : null;
}
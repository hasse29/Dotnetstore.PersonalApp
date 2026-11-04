using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dotnetstore.PersonalApp.Modules.Production.Features;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;

namespace Dotnetstore.PersonalApp.Modules.Production.Features.TransactionHistories;

internal sealed partial class TransactionHistoryEditorViewModel : ViewModelBase
{
    private readonly TransactionHistoryDto? _existing;
    private readonly CreateTransactionHistory.Handler _create;
    private readonly UpdateTransactionHistory.Handler _update;
    private readonly ILocalizationService _localization;

    public TransactionHistoryEditorViewModel(
        TransactionHistoryDto? existing,
        CreateTransactionHistory.Handler create,
        UpdateTransactionHistory.Handler update,
        IReadOnlyList<OptionDto> productOptions,
        TimeProvider clock,
        ILocalizationService localization)
    {
        _existing = existing;
        _create = create;
        _update = update;
        _localization = localization;

        ProductOptions = productOptions;
        TransactionTypeOptions =
        [
            new ChoiceOption("W", localization.Get("TransactionHistoryChoice_TransactionType_W")),
            new ChoiceOption("S", localization.Get("TransactionHistoryChoice_TransactionType_S")),
            new ChoiceOption("P", localization.Get("TransactionHistoryChoice_TransactionType_P")),
        ];

        if (existing is not null)
        {
            SelectedProduct = ProductOptions.FirstOrDefault(o => o.Id == existing.ProductId);
            ReferenceOrder = existing.ReferenceOrder ?? string.Empty;
            TransactionDate = existing.TransactionDate.ToDateTime(TimeOnly.MinValue);
            SelectedTransactionType = TransactionTypeOptions.FirstOrDefault(o => o.Code == existing.TransactionType);
            Quantity = existing.Quantity;
            ActualCost = existing.ActualCost;
        }
        else
        {
            TransactionDate = clock.GetLocalNow().Date;
            SelectedTransactionType = TransactionTypeOptions[0];
        }
    }

    public event EventHandler<TransactionHistoryDto>? Saved;

    public event EventHandler? Cancelled;

    public string Title => _localization.Get(_existing is null ? "TransactionHistoryEditor_New" : "TransactionHistoryEditor_Edit");

    public IReadOnlyList<OptionDto> ProductOptions { get; }

    [ObservableProperty]
    public partial OptionDto? SelectedProduct { get; set; }

    [ObservableProperty]
    public partial string ReferenceOrder { get; set; } = string.Empty;

    /// <summary>A <see cref="DateTime"/> because that is what the date picker binds to.</summary>
    [ObservableProperty]
    public partial DateTime? TransactionDate { get; set; }

    public IReadOnlyList<ChoiceOption> TransactionTypeOptions { get; }

    [ObservableProperty]
    public partial ChoiceOption? SelectedTransactionType { get; set; }

    [ObservableProperty]
    public partial decimal? Quantity { get; set; }

    [ObservableProperty]
    public partial decimal? ActualCost { get; set; }

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    [RelayCommand]
    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        ErrorMessage = null;

        var result = _existing is not { } existing
            ? await _create.HandleAsync(new CreateTransactionHistory.Command(SelectedProduct?.Id ?? Guid.Empty, ReferenceOrder, DateOnly.FromDateTime(TransactionDate ?? DateTime.MinValue), SelectedTransactionType?.Code ?? string.Empty, (int)(Quantity ?? 0), ActualCost ?? 0m), cancellationToken)
            : await _update.HandleAsync(new UpdateTransactionHistory.Command(existing.Id, SelectedProduct?.Id ?? Guid.Empty, ReferenceOrder, DateOnly.FromDateTime(TransactionDate ?? DateTime.MinValue), SelectedTransactionType?.Code ?? string.Empty, (int)(Quantity ?? 0), ActualCost ?? 0m), cancellationToken);

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
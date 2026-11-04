using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dotnetstore.PersonalApp.Modules.Production.Features;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;

namespace Dotnetstore.PersonalApp.Modules.Production.Features.WorkOrders;

internal sealed partial class WorkOrderEditorViewModel : ViewModelBase
{
    private readonly WorkOrderDto? _existing;
    private readonly CreateWorkOrder.Handler _create;
    private readonly UpdateWorkOrder.Handler _update;
    private readonly ILocalizationService _localization;

    public WorkOrderEditorViewModel(
        WorkOrderDto? existing,
        CreateWorkOrder.Handler create,
        UpdateWorkOrder.Handler update,
        IReadOnlyList<OptionDto> productOptions,
        IReadOnlyList<OptionDto> scrapReasonOptions,
        TimeProvider clock,
        ILocalizationService localization)
    {
        _existing = existing;
        _create = create;
        _update = update;
        _localization = localization;

        ProductOptions = productOptions;
        ScrapReasonOptions = [OptionDto.None(localization.Get("Production_None")), .. scrapReasonOptions];

        if (existing is not null)
        {
            Number = existing.Number;
            SelectedProduct = ProductOptions.FirstOrDefault(o => o.Id == existing.ProductId);
            OrderQty = existing.OrderQty;
            ScrappedQty = existing.ScrappedQty;
            StartDate = existing.StartDate.ToDateTime(TimeOnly.MinValue);
            EndDate = existing.EndDate?.ToDateTime(TimeOnly.MinValue);
            DueDate = existing.DueDate.ToDateTime(TimeOnly.MinValue);
            SelectedScrapReason = ScrapReasonOptions.FirstOrDefault(o => o.Id == existing.ScrapReasonId) ?? ScrapReasonOptions[0];
        }
        else
        {
            StartDate = clock.GetLocalNow().Date;
            DueDate = clock.GetLocalNow().Date;
            SelectedScrapReason = ScrapReasonOptions[0];
        }
    }

    public event EventHandler<WorkOrderDto>? Saved;

    public event EventHandler? Cancelled;

    public string Title => _localization.Get(_existing is null ? "WorkOrderEditor_New" : "WorkOrderEditor_Edit");

    [ObservableProperty]
    public partial string Number { get; set; } = string.Empty;

    public IReadOnlyList<OptionDto> ProductOptions { get; }

    [ObservableProperty]
    public partial OptionDto? SelectedProduct { get; set; }

    [ObservableProperty]
    public partial decimal? OrderQty { get; set; }

    [ObservableProperty]
    public partial decimal? ScrappedQty { get; set; }

    /// <summary>A <see cref="DateTime"/> because that is what the date picker binds to.</summary>
    [ObservableProperty]
    public partial DateTime? StartDate { get; set; }

    /// <summary>A <see cref="DateTime"/> because that is what the date picker binds to.</summary>
    [ObservableProperty]
    public partial DateTime? EndDate { get; set; }

    /// <summary>A <see cref="DateTime"/> because that is what the date picker binds to.</summary>
    [ObservableProperty]
    public partial DateTime? DueDate { get; set; }

    public IReadOnlyList<OptionDto> ScrapReasonOptions { get; }

    [ObservableProperty]
    public partial OptionDto? SelectedScrapReason { get; set; }

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    [RelayCommand]
    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        ErrorMessage = null;

        var result = _existing is not { } existing
            ? await _create.HandleAsync(new CreateWorkOrder.Command(Number, SelectedProduct?.Id ?? Guid.Empty, (int)(OrderQty ?? 0), (int)(ScrappedQty ?? 0), DateOnly.FromDateTime(StartDate ?? DateTime.MinValue), ToDate(EndDate), DateOnly.FromDateTime(DueDate ?? DateTime.MinValue), ToId(SelectedScrapReason)), cancellationToken)
            : await _update.HandleAsync(new UpdateWorkOrder.Command(existing.Id, Number, SelectedProduct?.Id ?? Guid.Empty, (int)(OrderQty ?? 0), (int)(ScrappedQty ?? 0), DateOnly.FromDateTime(StartDate ?? DateTime.MinValue), ToDate(EndDate), DateOnly.FromDateTime(DueDate ?? DateTime.MinValue), ToId(SelectedScrapReason)), cancellationToken);

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

    private static Guid? ToId(OptionDto? option) => option is { } o && o.Id != Guid.Empty ? o.Id : null;
}
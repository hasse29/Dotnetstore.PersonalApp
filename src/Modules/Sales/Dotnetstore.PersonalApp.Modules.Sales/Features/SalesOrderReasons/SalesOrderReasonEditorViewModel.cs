using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dotnetstore.PersonalApp.Modules.Sales.Features;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;

namespace Dotnetstore.PersonalApp.Modules.Sales.Features.SalesOrderReasons;

internal sealed partial class SalesOrderReasonEditorViewModel : ViewModelBase
{
    private readonly SalesOrderReasonDto? _existing;
    private readonly CreateSalesOrderReason.Handler _create;
    private readonly UpdateSalesOrderReason.Handler _update;
    private readonly ILocalizationService _localization;

    public SalesOrderReasonEditorViewModel(
        SalesOrderReasonDto? existing,
        CreateSalesOrderReason.Handler create,
        UpdateSalesOrderReason.Handler update,
        IReadOnlyList<OptionDto> salesOrderOptions,
        IReadOnlyList<OptionDto> salesReasonOptions,
        TimeProvider clock,
        ILocalizationService localization)
    {
        _existing = existing;
        _create = create;
        _update = update;
        _localization = localization;

        SalesOrderOptions = salesOrderOptions;
        SalesReasonOptions = salesReasonOptions;

        if (existing is not null)
        {
            SelectedSalesOrder = SalesOrderOptions.FirstOrDefault(o => o.Id == existing.SalesOrderId);
            SelectedSalesReason = SalesReasonOptions.FirstOrDefault(o => o.Id == existing.SalesReasonId);
        }
        else
        {
            // Every field starts empty.
        }
    }

    public event EventHandler<SalesOrderReasonDto>? Saved;

    public event EventHandler? Cancelled;

    public string Title => _localization.Get(_existing is null ? "SalesOrderReasonEditor_New" : "SalesOrderReasonEditor_Edit");

    public IReadOnlyList<OptionDto> SalesOrderOptions { get; }

    [ObservableProperty]
    public partial OptionDto? SelectedSalesOrder { get; set; }

    public IReadOnlyList<OptionDto> SalesReasonOptions { get; }

    [ObservableProperty]
    public partial OptionDto? SelectedSalesReason { get; set; }

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    [RelayCommand]
    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        ErrorMessage = null;

        var result = _existing is not { } existing
            ? await _create.HandleAsync(new CreateSalesOrderReason.Command(SelectedSalesOrder?.Id ?? Guid.Empty, SelectedSalesReason?.Id ?? Guid.Empty), cancellationToken)
            : await _update.HandleAsync(new UpdateSalesOrderReason.Command(existing.Id, SelectedSalesOrder?.Id ?? Guid.Empty, SelectedSalesReason?.Id ?? Guid.Empty), cancellationToken);

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
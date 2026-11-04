using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dotnetstore.PersonalApp.Modules.Sales.Features;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;

namespace Dotnetstore.PersonalApp.Modules.Sales.Features.SalesReasons;

internal sealed partial class SalesReasonEditorViewModel : ViewModelBase
{
    private readonly SalesReasonDto? _existing;
    private readonly CreateSalesReason.Handler _create;
    private readonly UpdateSalesReason.Handler _update;
    private readonly ILocalizationService _localization;

    public SalesReasonEditorViewModel(
        SalesReasonDto? existing,
        CreateSalesReason.Handler create,
        UpdateSalesReason.Handler update,
        TimeProvider clock,
        ILocalizationService localization)
    {
        _existing = existing;
        _create = create;
        _update = update;
        _localization = localization;

        ReasonTypeOptions =
        [
            new ChoiceOption("M", localization.Get("SalesReasonChoice_ReasonType_M")),
            new ChoiceOption("P", localization.Get("SalesReasonChoice_ReasonType_P")),
            new ChoiceOption("O", localization.Get("SalesReasonChoice_ReasonType_O")),
        ];

        if (existing is not null)
        {
            Name = existing.Name;
            SelectedReasonType = ReasonTypeOptions.FirstOrDefault(o => o.Code == existing.ReasonType);
        }
        else
        {
            SelectedReasonType = ReasonTypeOptions[0];
        }
    }

    public event EventHandler<SalesReasonDto>? Saved;

    public event EventHandler? Cancelled;

    public string Title => _localization.Get(_existing is null ? "SalesReasonEditor_New" : "SalesReasonEditor_Edit");

    [ObservableProperty]
    public partial string Name { get; set; } = string.Empty;

    public IReadOnlyList<ChoiceOption> ReasonTypeOptions { get; }

    [ObservableProperty]
    public partial ChoiceOption? SelectedReasonType { get; set; }

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    [RelayCommand]
    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        ErrorMessage = null;

        var result = _existing is not { } existing
            ? await _create.HandleAsync(new CreateSalesReason.Command(Name, SelectedReasonType?.Code ?? string.Empty), cancellationToken)
            : await _update.HandleAsync(new UpdateSalesReason.Command(existing.Id, Name, SelectedReasonType?.Code ?? string.Empty), cancellationToken);

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
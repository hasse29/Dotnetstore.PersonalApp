using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dotnetstore.PersonalApp.Modules.Production.Features;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;

namespace Dotnetstore.PersonalApp.Modules.Production.Features.WorkOrderRoutings;

internal sealed partial class WorkOrderRoutingEditorViewModel : ViewModelBase
{
    private readonly WorkOrderRoutingDto? _existing;
    private readonly CreateWorkOrderRouting.Handler _create;
    private readonly UpdateWorkOrderRouting.Handler _update;
    private readonly ILocalizationService _localization;

    public WorkOrderRoutingEditorViewModel(
        WorkOrderRoutingDto? existing,
        CreateWorkOrderRouting.Handler create,
        UpdateWorkOrderRouting.Handler update,
        IReadOnlyList<OptionDto> workOrderOptions,
        IReadOnlyList<OptionDto> locationOptions,
        TimeProvider clock,
        ILocalizationService localization)
    {
        _existing = existing;
        _create = create;
        _update = update;
        _localization = localization;

        WorkOrderOptions = workOrderOptions;
        LocationOptions = locationOptions;

        if (existing is not null)
        {
            SelectedWorkOrder = WorkOrderOptions.FirstOrDefault(o => o.Id == existing.WorkOrderId);
            OperationSequence = existing.OperationSequence;
            SelectedLocation = LocationOptions.FirstOrDefault(o => o.Id == existing.LocationId);
            ScheduledStartDate = existing.ScheduledStartDate.ToDateTime(TimeOnly.MinValue);
            ScheduledEndDate = existing.ScheduledEndDate.ToDateTime(TimeOnly.MinValue);
            ActualStartDate = existing.ActualStartDate?.ToDateTime(TimeOnly.MinValue);
            ActualEndDate = existing.ActualEndDate?.ToDateTime(TimeOnly.MinValue);
            ActualResourceHrs = existing.ActualResourceHrs;
            PlannedCost = existing.PlannedCost;
            ActualCost = existing.ActualCost;
        }
        else
        {
            ScheduledStartDate = clock.GetLocalNow().Date;
            ScheduledEndDate = clock.GetLocalNow().Date;
        }
    }

    public event EventHandler<WorkOrderRoutingDto>? Saved;

    public event EventHandler? Cancelled;

    public string Title => _localization.Get(_existing is null ? "WorkOrderRoutingEditor_New" : "WorkOrderRoutingEditor_Edit");

    public IReadOnlyList<OptionDto> WorkOrderOptions { get; }

    [ObservableProperty]
    public partial OptionDto? SelectedWorkOrder { get; set; }

    [ObservableProperty]
    public partial decimal? OperationSequence { get; set; }

    public IReadOnlyList<OptionDto> LocationOptions { get; }

    [ObservableProperty]
    public partial OptionDto? SelectedLocation { get; set; }

    /// <summary>A <see cref="DateTime"/> because that is what the date picker binds to.</summary>
    [ObservableProperty]
    public partial DateTime? ScheduledStartDate { get; set; }

    /// <summary>A <see cref="DateTime"/> because that is what the date picker binds to.</summary>
    [ObservableProperty]
    public partial DateTime? ScheduledEndDate { get; set; }

    /// <summary>A <see cref="DateTime"/> because that is what the date picker binds to.</summary>
    [ObservableProperty]
    public partial DateTime? ActualStartDate { get; set; }

    /// <summary>A <see cref="DateTime"/> because that is what the date picker binds to.</summary>
    [ObservableProperty]
    public partial DateTime? ActualEndDate { get; set; }

    [ObservableProperty]
    public partial decimal? ActualResourceHrs { get; set; }

    [ObservableProperty]
    public partial decimal? PlannedCost { get; set; }

    [ObservableProperty]
    public partial decimal? ActualCost { get; set; }

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    [RelayCommand]
    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        ErrorMessage = null;

        var result = _existing is not { } existing
            ? await _create.HandleAsync(new CreateWorkOrderRouting.Command(SelectedWorkOrder?.Id ?? Guid.Empty, (int)(OperationSequence ?? 0), SelectedLocation?.Id ?? Guid.Empty, DateOnly.FromDateTime(ScheduledStartDate ?? DateTime.MinValue), DateOnly.FromDateTime(ScheduledEndDate ?? DateTime.MinValue), ToDate(ActualStartDate), ToDate(ActualEndDate), ActualResourceHrs, PlannedCost ?? 0m, ActualCost), cancellationToken)
            : await _update.HandleAsync(new UpdateWorkOrderRouting.Command(existing.Id, SelectedWorkOrder?.Id ?? Guid.Empty, (int)(OperationSequence ?? 0), SelectedLocation?.Id ?? Guid.Empty, DateOnly.FromDateTime(ScheduledStartDate ?? DateTime.MinValue), DateOnly.FromDateTime(ScheduledEndDate ?? DateTime.MinValue), ToDate(ActualStartDate), ToDate(ActualEndDate), ActualResourceHrs, PlannedCost ?? 0m, ActualCost), cancellationToken);

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
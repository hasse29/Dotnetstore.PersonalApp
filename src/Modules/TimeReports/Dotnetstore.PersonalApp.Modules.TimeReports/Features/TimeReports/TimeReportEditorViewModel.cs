using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dotnetstore.PersonalApp.Modules.TimeReports.Features.WorkTimeTypes;
using Dotnetstore.PersonalApp.Modules.TimeReports.Persistence;
using Dotnetstore.PersonalApp.Shared.Clients;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;
using Dotnetstore.PersonalApp.Shared.Results;

namespace Dotnetstore.PersonalApp.Modules.TimeReports.Features.TimeReports;

internal sealed partial class TimeReportEditorViewModel : ViewModelBase
{
    private readonly TimeReportDto? _existing;
    private readonly bool _isCopy;
    private readonly Guid _userId;
    private readonly CreateTimeReport.Handler _create;
    private readonly UpdateTimeReport.Handler _update;
    private readonly ILocalizationService _localization;

    public TimeReportEditorViewModel(
        TimeReportDto? template,
        bool isCopy,
        Guid userId,
        Guid? fixedClientId,
        IReadOnlyList<ClientReference> clients,
        IReadOnlyList<WorkTimeTypeDto> workTimeTypes,
        DateOnly today,
        CreateTimeReport.Handler create,
        UpdateTimeReport.Handler update,
        ILocalizationService localization)
    {
        // A copy starts from the template's values but is saved as a new report.
        var existing = template;
        _existing = isCopy ? null : template;
        _isCopy = isCopy;
        _userId = userId;
        _create = create;
        _update = update;
        _localization = localization;
        Clients = clients;
        WorkTimeTypes = workTimeTypes;
        IsClientPickerVisible = fixedClientId is null;

        var clientId = existing?.ClientId ?? fixedClientId;
        SelectedClient = clients.FirstOrDefault(c => c.Id == clientId);

        SelectedWorkTimeType = workTimeTypes.FirstOrDefault(t => t.Id == (existing?.WorkTimeTypeId ?? WorkTimeTypeSeed.WorkId))
            ?? (existing is null && workTimeTypes.Count > 0 ? workTimeTypes[0] : null);

        if (existing is null)
        {
            Date = today.ToDateTime(TimeOnly.MinValue);
            DrivingKilometers = 0m;
        }
        else
        {
            Date = (isCopy ? existing.Date.AddDays(1) : existing.Date).ToDateTime(TimeOnly.MinValue);
            StartTime = existing.StartTime.ToTimeSpan();
            EndTime = existing.EndTime.ToTimeSpan();
            TotalHours = existing.TotalHours;
            Location = existing.Location ?? string.Empty;
            DrivingKilometers = existing.DrivingKilometers;
        }
    }

    public event EventHandler<TimeReportDto>? Saved;

    public event EventHandler? Cancelled;

    public IReadOnlyList<ClientReference> Clients { get; }

    public IReadOnlyList<WorkTimeTypeDto> WorkTimeTypes { get; }

    public bool IsClientPickerVisible { get; }

    public string Title => _localization.Get(_isCopy ? "TimeReportEditor_Copy" : _existing is null ? "TimeReportEditor_New" : "TimeReportEditor_Edit");

    [ObservableProperty]
    public partial ClientReference? SelectedClient { get; set; }

    [ObservableProperty]
    public partial WorkTimeTypeDto? SelectedWorkTimeType { get; set; }

    [ObservableProperty]
    public partial DateTime? Date { get; set; }

    [ObservableProperty]
    public partial TimeSpan? StartTime { get; set; }

    [ObservableProperty]
    public partial TimeSpan? EndTime { get; set; }

    [ObservableProperty]
    public partial decimal? TotalHours { get; set; }

    [ObservableProperty]
    public partial string Location { get; set; } = string.Empty;

    [ObservableProperty]
    public partial decimal? DrivingKilometers { get; set; }

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    [RelayCommand]
    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        ErrorMessage = null;

        var missing = FirstMissingField();
        if (missing is not null)
        {
            ErrorMessage = _localization.Describe(missing);
            return;
        }

        var clientId = SelectedClient!.Id;
        var workTimeTypeId = SelectedWorkTimeType!.Id;
        var date = DateOnly.FromDateTime(Date!.Value);
        var start = TimeOnly.FromTimeSpan(StartTime!.Value);
        var end = TimeOnly.FromTimeSpan(EndTime!.Value);
        var hours = TotalHours!.Value;
        var kilometers = DrivingKilometers ?? 0m;

        var result = _existing is null
            ? await _create.HandleAsync(
                new CreateTimeReport.Command(_userId, clientId, workTimeTypeId, date, start, end, hours, Location, kilometers), cancellationToken)
            : await _update.HandleAsync(
                new UpdateTimeReport.Command(_existing.Id, _userId, clientId, workTimeTypeId, date, start, end, hours, Location, kilometers), cancellationToken);

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

    // The pickers can be empty, which the handlers' value types cannot express, so the form reports those cases itself.
    private Error? FirstMissingField()
    {
        if (SelectedClient is null)
        {
            return Error.Validation("time_report_client_required", "Client is required.");
        }

        if (SelectedWorkTimeType is null)
        {
            return Error.Validation("time_report_work_time_type_required", "Work time type is required.");
        }

        if (Date is null)
        {
            return Error.Validation("time_report_date_required", "Date is required.");
        }

        if (StartTime is null)
        {
            return Error.Validation("time_report_start_time_required", "Start time is required.");
        }

        if (EndTime is null)
        {
            return Error.Validation("time_report_end_time_required", "End time is required.");
        }

        return TotalHours is null ? Error.Validation("time_report_total_hours_required", "Total hours is required.") : null;
    }
}
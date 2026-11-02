using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dotnetstore.PersonalApp.Shared.Clients;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;
using Dotnetstore.PersonalApp.Shared.Results;
using Dotnetstore.PersonalApp.Shared.Subjects;

namespace Dotnetstore.PersonalApp.Modules.Schedule.Features.Entries;

internal sealed partial class ScheduleEditorViewModel : ViewModelBase
{
    private static readonly TimeSpan DefaultStart = new(8, 0, 0);
    private static readonly TimeSpan DefaultEnd = new(9, 0, 0);

    private readonly ScheduleEntryDto? _existing;
    private readonly Guid _userId;
    private readonly CreateScheduleEntry.Handler _create;
    private readonly UpdateScheduleEntry.Handler _update;
    private readonly ILocalizationService _localization;

    public ScheduleEditorViewModel(
        ScheduleEntryDto? existing,
        Guid userId,
        IReadOnlyList<DayOption> days,
        IReadOnlyList<SubjectReference> subjects,
        IReadOnlyList<ClientReference> schools,
        CreateScheduleEntry.Handler create,
        UpdateScheduleEntry.Handler update,
        ILocalizationService localization)
    {
        _existing = existing;
        _userId = userId;
        Days = days;
        Subjects = subjects;
        Schools = schools;
        _create = create;
        _update = update;
        _localization = localization;

        if (existing is null)
        {
            SelectedDay = days[0];
            StartTime = DefaultStart;
            EndTime = DefaultEnd;
        }
        else
        {
            SelectedDay = days.First(d => d.Day == existing.Day);
            StartTime = existing.StartTime.ToTimeSpan();
            EndTime = existing.EndTime.ToTimeSpan();
            SelectedSubject = subjects.FirstOrDefault(s => s.Id == existing.SubjectId);
            SelectedSchool = schools.FirstOrDefault(c => c.Id == existing.ClientId);
            Room = existing.Room ?? string.Empty;
            Notes = existing.Notes ?? string.Empty;
        }
    }

    public event EventHandler<ScheduleEntryDto>? Saved;

    public event EventHandler? Cancelled;

    public string Title => _localization.Get(_existing is null ? "ScheduleEditor_New" : "ScheduleEditor_Edit");

    public IReadOnlyList<DayOption> Days { get; }

    public IReadOnlyList<SubjectReference> Subjects { get; }

    public IReadOnlyList<ClientReference> Schools { get; }

    [ObservableProperty]
    public partial DayOption? SelectedDay { get; set; }

    [ObservableProperty]
    public partial TimeSpan? StartTime { get; set; }

    [ObservableProperty]
    public partial TimeSpan? EndTime { get; set; }

    [ObservableProperty]
    public partial SubjectReference? SelectedSubject { get; set; }

    [ObservableProperty]
    public partial ClientReference? SelectedSchool { get; set; }

    [ObservableProperty]
    public partial string Room { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Notes { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    [RelayCommand]
    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        ErrorMessage = null;

        if (SelectedDay is not { } day || StartTime is not { } start || EndTime is not { } end)
        {
            ErrorMessage = _localization.Describe(Error.Validation("schedule_time_required", "Day and times are required."));
            return;
        }

        if (SelectedSubject is not { } subject)
        {
            ErrorMessage = _localization.Describe(Error.Validation("schedule_subject_required", "A subject is required."));
            return;
        }

        if (SelectedSchool is not { } school)
        {
            ErrorMessage = _localization.Describe(Error.Validation("schedule_school_required", "A school is required."));
            return;
        }

        var startTime = TimeOnly.FromTimeSpan(start);
        var endTime = TimeOnly.FromTimeSpan(end);

        var result = _existing is null
            ? await _create.HandleAsync(
                new CreateScheduleEntry.Command(_userId, day.Day, startTime, endTime, subject.Id, school.Id, Room, Notes),
                cancellationToken)
            : await _update.HandleAsync(
                new UpdateScheduleEntry.Command(_existing.Id, _userId, day.Day, startTime, endTime, subject.Id, school.Id, Room, Notes),
                cancellationToken);

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

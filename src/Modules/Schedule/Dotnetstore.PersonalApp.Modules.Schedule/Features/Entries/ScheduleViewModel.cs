using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dotnetstore.PersonalApp.Shared.Clients;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;
using Dotnetstore.PersonalApp.Shared.Subjects;

namespace Dotnetstore.PersonalApp.Modules.Schedule.Features.Entries;

internal sealed partial class ScheduleViewModel(
    Guid userId,
    ListScheduleEntries.Handler list,
    CreateScheduleEntry.Handler create,
    UpdateScheduleEntry.Handler update,
    DeleteScheduleEntry.Handler delete,
    ISubjectLookup subjectLookup,
    IClientLookup clientLookup,
    ILocalizationService localization) : ViewModelBase
{
    private IReadOnlyList<SubjectReference> _subjects = [];
    private IReadOnlyList<ClientReference> _schools = [];

    public ObservableCollection<ScheduleRow> Rows { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RequestDeleteCommand))]
    [NotifyCanExecuteChangedFor(nameof(ConfirmDeleteCommand))]
    public partial ScheduleRow? SelectedRow { get; set; }

    [ObservableProperty]
    public partial ScheduleEditorViewModel? Editor { get; set; }

    [ObservableProperty]
    public partial string? StatusMessage { get; set; }

    [ObservableProperty]
    public partial bool IsConfirmingDelete { get; set; }

    partial void OnSelectedRowChanged(ScheduleRow? value)
    {
        IsConfirmingDelete = false;
        if (value is not null)
        {
            OpenEditor(value.Entry);
        }
    }

    [RelayCommand]
    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        _subjects = await subjectLookup.ListAsync(cancellationToken);
        _schools = await clientLookup.ListAsync(cancellationToken);
        var entries = await list.HandleAsync(new ListScheduleEntries.Query(userId), cancellationToken);

        var days = DayOption.All(localization).ToDictionary(d => d.Day);
        var subjectNames = _subjects.ToDictionary(s => s.Id, s => s.Name);
        var schoolNames = _schools.ToDictionary(c => c.Id, c => c.Name);

        Rows.Clear();
        foreach (var entry in entries)
        {
            Rows.Add(new ScheduleRow(
                entry,
                days[entry.Day].Name,
                subjectNames.GetValueOrDefault(entry.SubjectId, "?"),
                schoolNames.GetValueOrDefault(entry.ClientId, "?")));
        }

        StatusMessage = localization.Format("Schedule_Count", Rows.Count);
    }

    [RelayCommand]
    private void New()
    {
        // A lesson needs a subject and a school to refer to, so point the user at where to create them.
        if (_subjects.Count == 0 || _schools.Count == 0)
        {
            StatusMessage = localization.Get("Schedule_NeedSubjectsAndSchools");
            return;
        }

        SelectedRow = null;
        OpenEditor(null);
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void RequestDelete() => IsConfirmingDelete = true;

    [RelayCommand]
    private void CancelDelete() => IsConfirmingDelete = false;

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private async Task ConfirmDeleteAsync(CancellationToken cancellationToken)
    {
        if (SelectedRow is not { } row)
        {
            return;
        }

        var result = await delete.HandleAsync(new DeleteScheduleEntry.Command(row.Entry.Id, userId), cancellationToken);

        CloseEditor();
        await LoadAsync(cancellationToken);
        if (!result.IsSuccess)
        {
            StatusMessage = localization.Describe(result.Error);
        }
    }

    private bool HasSelection() => SelectedRow is not null;

    private void OpenEditor(ScheduleEntryDto? existing)
    {
        DetachEditor();
        var editor = new ScheduleEditorViewModel(
            existing, userId, DayOption.All(localization), _subjects, _schools, create, update, localization);
        Editor = editor;
        editor.Saved += OnEditorSaved;
        editor.Cancelled += OnEditorCancelled;
    }

    private void CloseEditor()
    {
        DetachEditor();
        Editor = null;
        SelectedRow = null;
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
    private async void OnEditorSaved(object? sender, ScheduleEntryDto saved)
    {
        CloseEditor();
        await LoadAsync(CancellationToken.None);
        StatusMessage = localization.Get("Schedule_Saved");
    }

    private void OnEditorCancelled(object? sender, EventArgs e) => CloseEditor();
}

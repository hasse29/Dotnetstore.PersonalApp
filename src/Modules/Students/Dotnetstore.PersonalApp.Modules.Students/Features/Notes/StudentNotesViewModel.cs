using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;
using Dotnetstore.PersonalApp.Shared.Results;

namespace Dotnetstore.PersonalApp.Modules.Students.Features.Notes;

/// <summary>A student's running notes: the form on top adds an entry, picking one in the list edits or deletes it. The list shows the latest entry first.</summary>
internal sealed partial class StudentNotesViewModel(
    Guid studentId,
    ListStudentNotes.Handler list,
    AddStudentNote.Handler add,
    UpdateStudentNote.Handler update,
    DeleteStudentNote.Handler delete,
    ILocalizationService localization,
    TimeProvider clock) : ViewModelBase
{
    private bool _restoringSelection;

    public ObservableCollection<StudentNoteDto> Notes { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RequestDeleteCommand))]
    [NotifyCanExecuteChangedFor(nameof(ConfirmDeleteCommand))]
    public partial StudentNoteDto? SelectedNote { get; set; }

    /// <summary>The form's date; a <see cref="DateTime"/> because that is what the date picker binds to.</summary>
    [ObservableProperty]
    public partial DateTime? Date { get; set; } = clock.GetLocalNow().Date;

    [ObservableProperty]
    public partial string Text { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    [ObservableProperty]
    public partial string? StatusMessage { get; set; }

    [ObservableProperty]
    public partial bool IsConfirmingDelete { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FormTitle))]
    public partial bool IsEditing { get; set; }

    public string FormTitle => localization.Get(IsEditing ? "StudentNotes_EditEntry" : "StudentNotes_NewEntry");

    partial void OnSelectedNoteChanged(StudentNoteDto? value)
    {
        if (_restoringSelection)
        {
            return;
        }

        IsConfirmingDelete = false;
        ErrorMessage = null;
        if (value is null)
        {
            return;
        }

        IsEditing = true;
        Date = value.Date.ToDateTime(TimeOnly.MinValue);
        Text = value.Text;
    }

    [RelayCommand]
    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        var notes = await list.HandleAsync(new ListStudentNotes.Query(studentId), cancellationToken);

        var selectedId = SelectedNote?.Id;
        _restoringSelection = true;
        try
        {
            Notes.Clear();
            foreach (var note in notes)
            {
                Notes.Add(note);
            }

            SelectedNote = Notes.FirstOrDefault(n => n.Id == selectedId);
        }
        finally
        {
            _restoringSelection = false;
        }

        StatusMessage = localization.Format("StudentNotes_Count", notes.Count);
    }

    [RelayCommand]
    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        ErrorMessage = null;

        var date = DateOnly.FromDateTime(Date ?? clock.GetLocalNow().Date);
        var result = SelectedNote is { } existing
            ? await update.HandleAsync(new UpdateStudentNote.Command(existing.Id, date, Text), cancellationToken)
            : await add.HandleAsync(new AddStudentNote.Command(studentId, date, Text), cancellationToken);

        if (!result.IsSuccess)
        {
            ErrorMessage = localization.Describe(result.Error);
            return;
        }

        ClearForm();
        await LoadAsync(cancellationToken);
    }

    /// <summary>Drops the selection and empties the form for a new entry; the date goes back to today.</summary>
    [RelayCommand]
    private void ClearForm()
    {
        SelectedNote = null;
        IsEditing = false;
        IsConfirmingDelete = false;
        ErrorMessage = null;
        Date = clock.GetLocalNow().Date;
        Text = string.Empty;
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void RequestDelete() => IsConfirmingDelete = true;

    [RelayCommand]
    private void CancelDelete() => IsConfirmingDelete = false;

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private async Task ConfirmDeleteAsync(CancellationToken cancellationToken)
    {
        if (SelectedNote is not { } note)
        {
            return;
        }

        var result = await delete.HandleAsync(new DeleteStudentNote.Command(note.Id), cancellationToken);

        ClearForm();
        await LoadAsync(cancellationToken);
        if (!result.IsSuccess)
        {
            StatusMessage = localization.Describe(result.Error);
        }
    }

    private bool HasSelection() => SelectedNote is not null;
}

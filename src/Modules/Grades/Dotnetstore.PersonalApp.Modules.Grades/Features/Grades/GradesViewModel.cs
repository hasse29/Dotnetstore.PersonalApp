using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dotnetstore.PersonalApp.Modules.Grades.Features.Scales;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;
using Dotnetstore.PersonalApp.Shared.Students;
using Dotnetstore.PersonalApp.Shared.Subjects;

namespace Dotnetstore.PersonalApp.Modules.Grades.Features.Grades;

/// <summary>
/// The signed-in teacher's grades for one student (a tab on the student, <paramref name="studentId"/> set) or for every student
/// (the module's own page, <paramref name="studentId"/> null, where the editor has a student picker): list and editor.
/// </summary>
internal sealed partial class GradesViewModel(
    Guid userId,
    Guid? studentId,
    ListGrades.Handler list,
    CreateGrade.Handler create,
    UpdateGrade.Handler update,
    DeleteGrade.Handler delete,
    ListGradeScales.Handler listScales,
    IStudentLookup studentLookup,
    ISubjectLookup subjectLookup,
    ILocalizationService localization,
    TimeProvider clock) : ViewModelBase
{
    private IReadOnlyList<StudentReference> _students = [];
    private IReadOnlyList<SubjectReference> _subjects = [];
    private IReadOnlyList<GradeScaleDto> _scales = [];
    private bool _restoringSelection;

    /// <summary>True on the module's own page, false as a tab on a student; the page adds a title and a student column.</summary>
    public bool IsPageLayout => studentId is null;

    public ObservableCollection<GradeRow> Rows { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RequestDeleteCommand))]
    [NotifyCanExecuteChangedFor(nameof(ConfirmDeleteCommand))]
    public partial GradeRow? SelectedRow { get; set; }

    [ObservableProperty]
    public partial GradeEditorViewModel? Editor { get; set; }

    [ObservableProperty]
    public partial string? StatusMessage { get; set; }

    [ObservableProperty]
    public partial bool IsConfirmingDelete { get; set; }

    partial void OnSelectedRowChanged(GradeRow? value)
    {
        if (_restoringSelection)
        {
            return;
        }

        IsConfirmingDelete = false;
        if (value is not null)
        {
            OpenEditor(new GradeEditorViewModel(userId, studentId, value.Grade, _students, _subjects, _scales, create, update, localization, clock));
        }
    }

    [RelayCommand]
    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        _students = await studentLookup.ListAsync(cancellationToken);
        _subjects = await subjectLookup.ListAsync(cancellationToken);
        _scales = await listScales.HandleAsync(new ListGradeScales.Query(), cancellationToken);
        var grades = await list.HandleAsync(new ListGrades.Query(userId, studentId), cancellationToken);

        var studentNames = _students.ToDictionary(s => s.Id, s => s.Name);
        var subjectNames = _subjects.ToDictionary(s => s.Id, s => s.Name);

        // Reloading must not reopen the editor, so the selection is put back without the change handler acting on it.
        var selectedId = SelectedRow?.Grade.Id;
        _restoringSelection = true;
        try
        {
            Rows.Clear();
            foreach (var grade in grades)
            {
                Rows.Add(new GradeRow(grade, studentNames.GetValueOrDefault(grade.StudentId, "?"), subjectNames.GetValueOrDefault(grade.SubjectId, "?")));
            }

            SelectedRow = Rows.FirstOrDefault(r => r.Grade.Id == selectedId);
        }
        finally
        {
            _restoringSelection = false;
        }

        StatusMessage = localization.Format("Grades_Count", Rows.Count);
    }

    [RelayCommand]
    private void New()
    {
        // A grade needs a subject and a scale to refer to, so point the user at where to create them.
        if (_subjects.Count == 0 || _scales.Count == 0)
        {
            StatusMessage = localization.Get("Grades_NeedSubjectAndScale");
            return;
        }

        SelectedRow = null;
        OpenEditor(new GradeEditorViewModel(userId, studentId, null, _students, _subjects, _scales, create, update, localization, clock));
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

        var result = await delete.HandleAsync(new DeleteGrade.Command(row.Grade.Id, userId), cancellationToken);

        CloseEditor();
        await LoadAsync(cancellationToken);
        if (!result.IsSuccess)
        {
            StatusMessage = localization.Describe(result.Error);
        }
    }

    private bool HasSelection() => SelectedRow is not null;

    private void OpenEditor(GradeEditorViewModel editor)
    {
        DetachEditor();
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
    private async void OnEditorSaved(object? sender, GradeDto saved)
    {
        CloseEditor();
        await LoadAsync(CancellationToken.None);
        StatusMessage = localization.Format("Grades_Saved", saved.GradeLabel);
    }

    private void OnEditorCancelled(object? sender, EventArgs e) => CloseEditor();
}

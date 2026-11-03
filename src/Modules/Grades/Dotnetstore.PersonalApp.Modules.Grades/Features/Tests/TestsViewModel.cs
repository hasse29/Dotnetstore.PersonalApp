using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dotnetstore.PersonalApp.Modules.Grades.Features.Scales;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;
using Dotnetstore.PersonalApp.Shared.Students;

namespace Dotnetstore.PersonalApp.Modules.Grades.Features.Tests;

/// <summary>
/// The signed-in teacher's tests: a list, an editor for the selected test (name, date, maximum score) and,
/// once a test exists, the students' results on it.
/// </summary>
internal sealed partial class TestsViewModel(
    Guid userId,
    ListTests.Handler list,
    CreateTest.Handler create,
    UpdateTest.Handler update,
    DeleteTest.Handler delete,
    ListTestResults.Handler listResults,
    SaveTestResult.Handler saveResult,
    DeleteTestResult.Handler deleteResult,
    ListGradeScales.Handler listScales,
    IStudentLookup studentLookup,
    ILocalizationService localization,
    TimeProvider clock) : ViewModelBase
{
    private IReadOnlyList<StudentReference> _students = [];
    private IReadOnlyList<GradeScaleDto> _scales = [];
    private bool _restoringSelection;

    public ObservableCollection<TestDto> Rows { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RequestDeleteCommand))]
    [NotifyCanExecuteChangedFor(nameof(ConfirmDeleteCommand))]
    public partial TestDto? SelectedTest { get; set; }

    [ObservableProperty]
    public partial TestEditorViewModel? Editor { get; set; }

    /// <summary>The results of the selected test; null while a new test is being created, since it has none yet.</summary>
    [ObservableProperty]
    public partial TestResultsViewModel? Results { get; set; }

    [ObservableProperty]
    public partial string? StatusMessage { get; set; }

    [ObservableProperty]
    public partial bool IsConfirmingDelete { get; set; }

    partial void OnSelectedTestChanged(TestDto? value)
    {
        if (_restoringSelection)
        {
            return;
        }

        IsConfirmingDelete = false;
        if (value is not null)
        {
            OpenEditor(new TestEditorViewModel(userId, value, create, update, localization, clock));
            var results = new TestResultsViewModel(userId, value, _students, _scales, listResults, saveResult, deleteResult, localization);
            results.Changed += OnResultsChanged;
            Results = results;
        }
    }

    [RelayCommand]
    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        _students = await studentLookup.ListAsync(cancellationToken);
        _scales = await listScales.HandleAsync(new ListGradeScales.Query(), cancellationToken);
        var tests = await list.HandleAsync(new ListTests.Query(userId), cancellationToken);

        // Reloading must not reopen the editor, so the selection is put back without the change handler acting on it.
        var selectedId = SelectedTest?.Id;
        _restoringSelection = true;
        try
        {
            Rows.Clear();
            foreach (var test in tests)
            {
                Rows.Add(test);
            }

            SelectedTest = Rows.FirstOrDefault(t => t.Id == selectedId);
        }
        finally
        {
            _restoringSelection = false;
        }

        StatusMessage = localization.Format("Tests_Count", Rows.Count);
    }

    [RelayCommand]
    private void New()
    {
        SelectedTest = null;
        Results = null;
        OpenEditor(new TestEditorViewModel(userId, null, create, update, localization, clock));
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void RequestDelete() => IsConfirmingDelete = true;

    [RelayCommand]
    private void CancelDelete() => IsConfirmingDelete = false;

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private async Task ConfirmDeleteAsync(CancellationToken cancellationToken)
    {
        if (SelectedTest is not { } test)
        {
            return;
        }

        var result = await delete.HandleAsync(new DeleteTest.Command(test.Id, userId), cancellationToken);

        CloseEditor();
        await LoadAsync(cancellationToken);
        if (!result.IsSuccess)
        {
            StatusMessage = localization.Describe(result.Error);
        }
    }

    private bool HasSelection() => SelectedTest is not null;

    private void OpenEditor(TestEditorViewModel editor)
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
        Results = null;
        SelectedTest = null;
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
    private async void OnEditorSaved(object? sender, TestDto saved)
    {
        // The saved test stays selected, so the teacher goes straight on to entering results.
        SelectedTest = null;
        DetachEditor();
        await LoadAsync(CancellationToken.None);
        SelectedTest = Rows.FirstOrDefault(t => t.Id == saved.Id);
        StatusMessage = localization.Format("Tests_Saved", saved.Name);
    }

    // Event handler, so it has to be async void; the reload only refreshes the result counts and keeps the selection.
    private async void OnResultsChanged(object? sender, EventArgs e) => await LoadAsync(CancellationToken.None);

    private void OnEditorCancelled(object? sender, EventArgs e) => CloseEditor();
}

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dotnetstore.PersonalApp.Modules.Grades.Features.Scales;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;
using Dotnetstore.PersonalApp.Shared.Students;

namespace Dotnetstore.PersonalApp.Modules.Grades.Features.Tests;

/// <summary>The students' results on one test (score and grade) with a small form to set them.</summary>
internal sealed partial class TestResultsViewModel(
    Guid userId,
    TestDto test,
    IReadOnlyList<StudentReference> students,
    IReadOnlyList<GradeScaleDto> scales,
    ListTestResults.Handler list,
    SaveTestResult.Handler save,
    DeleteTestResult.Handler delete,
    ILocalizationService localization) : ViewModelBase
{
    private bool _filling;

    /// <summary>Raised after a result was saved or deleted, so the test list can refresh its result count.</summary>
    public event EventHandler? Changed;

    public ObservableCollection<TestResultRow> Rows { get; } = [];

    public IReadOnlyList<StudentReference> Students { get; } = students;

    public IReadOnlyList<GradeScaleDto> Scales { get; } = scales;

    /// <summary>The steps of the chosen scale, best first.</summary>
    public ObservableCollection<GradeScaleItemDto> Items { get; } = [];

    public string ScoreLabel => localization.Format("TestResultField_Score", ScoreText.Format(test.MaxScore));

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(DeleteCommand))]
    public partial TestResultRow? SelectedRow { get; set; }

    [ObservableProperty]
    public partial StudentReference? SelectedStudent { get; set; }

    [ObservableProperty]
    public partial string ScoreInput { get; set; } = string.Empty;

    [ObservableProperty]
    public partial GradeScaleDto? SelectedScale { get; set; }

    [ObservableProperty]
    public partial GradeScaleItemDto? SelectedItem { get; set; }

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    [ObservableProperty]
    public partial string? StatusMessage { get; set; }

    partial void OnSelectedScaleChanged(GradeScaleDto? value)
    {
        Items.Clear();
        SelectedItem = null;
        foreach (var item in value?.Items ?? [])
        {
            Items.Add(item);
        }
    }

    partial void OnSelectedRowChanged(TestResultRow? value)
    {
        if (_filling || value is null)
        {
            return;
        }

        // Picking a row puts its result in the form; the scale goes first because choosing it clears the grade.
        ErrorMessage = null;
        SelectedStudent = Students.FirstOrDefault(s => s.Id == value.Result.StudentId);
        ScoreInput = ScoreText.Format(value.Result.Score);
        SelectedScale = Scales.FirstOrDefault(s => s.Id == value.Result.ScaleId);
        SelectedItem = Items.FirstOrDefault(i => i.Id == value.Result.ScaleItemId);
    }

    [RelayCommand]
    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        var result = await list.HandleAsync(new ListTestResults.Query(test.Id, userId), cancellationToken);
        if (!result.IsSuccess)
        {
            StatusMessage = localization.Describe(result.Error);
            return;
        }

        var names = Students.ToDictionary(s => s.Id, s => s.Name);

        _filling = true;
        try
        {
            SelectedRow = null;
            Rows.Clear();
            foreach (var row in result.Value
                .Select(r => new TestResultRow(r, names.GetValueOrDefault(r.StudentId, "?"), test.MaxScore))
                .OrderBy(r => r.StudentName, StringComparer.CurrentCultureIgnoreCase))
            {
                Rows.Add(row);
            }
        }
        finally
        {
            _filling = false;
        }

        // With only one grading system there is nothing to choose, so the form starts on it.
        if (SelectedScale is null && Scales.Count == 1)
        {
            SelectedScale = Scales[0];
        }

        StatusMessage = localization.Format("TestResults_Count", Rows.Count);
    }

    [RelayCommand]
    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        ErrorMessage = null;

        var result = await save.HandleAsync(
            new SaveTestResult.Command(test.Id, userId, SelectedStudent?.Id ?? Guid.Empty, ScoreText.Parse(ScoreInput), SelectedItem?.Id),
            cancellationToken);

        if (!result.IsSuccess)
        {
            ErrorMessage = localization.Describe(result.Error);
            return;
        }

        var studentName = SelectedStudent?.Name ?? "?";
        Clear();
        await LoadAsync(cancellationToken);
        StatusMessage = localization.Format("TestResults_Saved", studentName);
        Changed?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void Clear()
    {
        _filling = true;
        try
        {
            SelectedRow = null;
            SelectedStudent = null;
            ScoreInput = string.Empty;
            SelectedScale = Scales.Count == 1 ? Scales[0] : null;
            ErrorMessage = null;
        }
        finally
        {
            _filling = false;
        }
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private async Task DeleteAsync(CancellationToken cancellationToken)
    {
        if (SelectedRow is not { } row)
        {
            return;
        }

        var result = await delete.HandleAsync(new DeleteTestResult.Command(row.Result.Id, userId), cancellationToken);

        Clear();
        await LoadAsync(cancellationToken);
        if (!result.IsSuccess)
        {
            StatusMessage = localization.Describe(result.Error);
        }

        Changed?.Invoke(this, EventArgs.Empty);
    }

    private bool HasSelection() => SelectedRow is not null;
}

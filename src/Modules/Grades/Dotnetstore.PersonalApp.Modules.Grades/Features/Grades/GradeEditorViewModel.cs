using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dotnetstore.PersonalApp.Modules.Grades.Features.Scales;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;
using Dotnetstore.PersonalApp.Shared.Results;
using Dotnetstore.PersonalApp.Shared.Students;
using Dotnetstore.PersonalApp.Shared.Subjects;

namespace Dotnetstore.PersonalApp.Modules.Grades.Features.Grades;

internal sealed partial class GradeEditorViewModel : ViewModelBase
{
    private readonly Guid _userId;
    private readonly Guid? _studentId;
    private readonly GradeDto? _existing;
    private readonly CreateGrade.Handler _create;
    private readonly UpdateGrade.Handler _update;
    private readonly ILocalizationService _localization;

    /// <param name="studentId">The student when the editor sits in a student's tab; null on the Grades page, where the user picks one.</param>
    /// <param name="students">The students to pick from when <paramref name="studentId"/> is null.</param>
    public GradeEditorViewModel(
        Guid userId,
        Guid? studentId,
        GradeDto? existing,
        IReadOnlyList<StudentReference> students,
        IReadOnlyList<SubjectReference> subjects,
        IReadOnlyList<GradeScaleDto> scales,
        CreateGrade.Handler create,
        UpdateGrade.Handler update,
        ILocalizationService localization,
        TimeProvider clock)
    {
        _userId = userId;
        _studentId = studentId;
        _existing = existing;
        _create = create;
        _update = update;
        _localization = localization;
        Students = students;
        Subjects = subjects;
        Scales = scales;
        Date = clock.GetLocalNow().Date;

        if (existing is not null)
        {
            SelectedSubject = subjects.FirstOrDefault(s => s.Id == existing.SubjectId);
            SelectedScale = scales.FirstOrDefault(s => s.Id == existing.ScaleId);
            SelectedItem = Items.FirstOrDefault(i => i.Id == existing.ScaleItemId);
            Date = existing.Date.ToDateTime(TimeOnly.MinValue);
            Comment = existing.Comment ?? string.Empty;
        }
        else if (scales.Count == 1)
        {
            SelectedScale = scales[0];
        }
    }

    public event EventHandler<GradeDto>? Saved;

    public event EventHandler? Cancelled;

    public string Title => _localization.Get(_existing is null ? "GradeEditor_New" : "GradeEditor_Edit");

    public IReadOnlyList<StudentReference> Students { get; }

    public IReadOnlyList<SubjectReference> Subjects { get; }

    public IReadOnlyList<GradeScaleDto> Scales { get; }

    /// <summary>The steps of the chosen scale, best first.</summary>
    public ObservableCollection<GradeScaleItemDto> Items { get; } = [];

    /// <summary>A grade stays with its student, so the picker is only for new grades outside a student's tab.</summary>
    public bool ShowStudentPicker => _studentId is null && _existing is null;

    [ObservableProperty]
    public partial StudentReference? SelectedStudent { get; set; }

    [ObservableProperty]
    public partial SubjectReference? SelectedSubject { get; set; }

    [ObservableProperty]
    public partial GradeScaleDto? SelectedScale { get; set; }

    [ObservableProperty]
    public partial GradeScaleItemDto? SelectedItem { get; set; }

    /// <summary>A <see cref="DateTime"/> because that is what the date picker binds to.</summary>
    [ObservableProperty]
    public partial DateTime? Date { get; set; }

    [ObservableProperty]
    public partial string Comment { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    partial void OnSelectedScaleChanged(GradeScaleDto? value)
    {
        Items.Clear();
        SelectedItem = null;
        foreach (var item in value?.Items ?? [])
        {
            Items.Add(item);
        }
    }

    [RelayCommand]
    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        ErrorMessage = null;

        var date = DateOnly.FromDateTime(Date ?? DateTime.MinValue);
        var subjectId = SelectedSubject?.Id ?? Guid.Empty;
        var itemId = SelectedItem?.Id ?? Guid.Empty;

        Result<GradeDto> result;
        if (_existing is not null)
        {
            result = await _update.HandleAsync(new UpdateGrade.Command(_existing.Id, _userId, subjectId, itemId, date, Comment), cancellationToken);
        }
        else
        {
            // The create handler reports an empty id as "choose a student", the same way it does for subject and grade.
            result = await _create.HandleAsync(
                new CreateGrade.Command(_userId, _studentId ?? SelectedStudent?.Id ?? Guid.Empty, subjectId, itemId, date, Comment),
                cancellationToken);
        }

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

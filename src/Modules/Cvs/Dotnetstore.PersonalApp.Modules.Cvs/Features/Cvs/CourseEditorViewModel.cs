using CommunityToolkit.Mvvm.ComponentModel;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Results;

namespace Dotnetstore.PersonalApp.Modules.Cvs.Features.Cvs;

internal sealed record CourseDraft(Guid? Id, string Name, string? Provider, int? CompletedYear);

internal sealed partial class CourseEditorViewModel : CvSectionEditorViewModel
{
    private readonly Guid? _id;
    private readonly Func<CourseDraft, CancellationToken, Task<Error?>> _save;
    private readonly Func<CancellationToken, Task<Error?>>? _delete;

    public CourseEditorViewModel(
        CourseDto? existing,
        Func<CourseDraft, CancellationToken, Task<Error?>> save,
        Func<CancellationToken, Task<Error?>>? delete,
        ILocalizationService localization)
        : base(localization)
    {
        _id = existing?.Id;
        _save = save;
        _delete = delete;

        if (existing is not null)
        {
            Name = existing.Name;
            Provider = existing.Provider ?? string.Empty;
            CompletedYear = existing.CompletedYear;
        }
    }

    public override string Title => Localization.Get(_id is null ? "CvCourseEditor_New" : "CvCourseEditor_Edit");

    public override bool CanDelete => _id is not null;

    [ObservableProperty]
    public partial string Name { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Provider { get; set; } = string.Empty;

    /// <summary>A decimal because that is what NumericUpDown binds to.</summary>
    [ObservableProperty]
    public partial decimal? CompletedYear { get; set; }

    protected override Task<Error?> SaveCoreAsync(CancellationToken cancellationToken) =>
        _save(new CourseDraft(_id, Name, Provider, CompletedYear is { } year ? (int)year : null), cancellationToken);

    protected override Task<Error?> DeleteCoreAsync(CancellationToken cancellationToken) =>
        _delete is null ? Task.FromResult<Error?>(null) : _delete(cancellationToken);
}

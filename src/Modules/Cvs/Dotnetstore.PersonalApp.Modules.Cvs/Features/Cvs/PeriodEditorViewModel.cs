using CommunityToolkit.Mvvm.ComponentModel;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Results;

namespace Dotnetstore.PersonalApp.Modules.Cvs.Features.Cvs;

internal enum PeriodKind
{
    Work,
    Education,
}

/// <param name="Organisation">The employer or the school.</param>
/// <param name="Position">The role or the programme.</param>
internal sealed record PeriodDraft(Guid? Id, string Organisation, string Position, DateOnly StartDate, DateOnly? EndDate, string? Description, IReadOnlyList<string> Technologies);

/// <summary>Edits one job or one education: both are an organisation, a position, a period and a description.</summary>
internal sealed partial class PeriodEditorViewModel : CvSectionEditorViewModel
{
    private readonly PeriodKind _kind;
    private readonly Guid? _id;
    private readonly Func<PeriodDraft, CancellationToken, Task<Error?>> _save;
    private readonly Func<CancellationToken, Task<Error?>>? _delete;

    public PeriodEditorViewModel(
        PeriodKind kind,
        PeriodPrefill? existing,
        Func<PeriodDraft, CancellationToken, Task<Error?>> save,
        Func<CancellationToken, Task<Error?>>? delete,
        ILocalizationService localization)
        : base(localization)
    {
        _kind = kind;
        _id = existing?.Id;
        _save = save;
        _delete = delete;

        if (existing is not null)
        {
            Organisation = existing.Organisation;
            Position = existing.Position;
            StartDate = existing.StartDate.ToDateTime(TimeOnly.MinValue);
            EndDate = existing.EndDate?.ToDateTime(TimeOnly.MinValue);
            IsOngoing = existing.EndDate is null;
            Description = existing.Description ?? string.Empty;
            Technologies = string.Join(", ", existing.Technologies);
        }
    }

    public override string Title => Localization.Get((_kind, _id) switch
    {
        (PeriodKind.Work, null) => "CvWorkEditor_New",
        (PeriodKind.Work, _) => "CvWorkEditor_Edit",
        (_, null) => "CvEducationEditor_New",
        _ => "CvEducationEditor_Edit",
    });

    public override bool CanDelete => _id is not null;

    /// <summary>Only a job has technology tags; an education does not.</summary>
    public bool ShowTechnologies => _kind == PeriodKind.Work;

    public string OrganisationLabel => Localization.Get(_kind == PeriodKind.Work ? "CvField_Employer" : "CvField_School");

    public string PositionLabel => Localization.Get(_kind == PeriodKind.Work ? "CvField_Role" : "CvField_Programme");

    [ObservableProperty]
    public partial string Organisation { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Position { get; set; } = string.Empty;

    [ObservableProperty]
    public partial DateTime? StartDate { get; set; }

    [ObservableProperty]
    public partial DateTime? EndDate { get; set; }

    [ObservableProperty]
    public partial bool IsOngoing { get; set; }

    [ObservableProperty]
    public partial string Description { get; set; } = string.Empty;

    /// <summary>The tags as typed, separated by commas.</summary>
    [ObservableProperty]
    public partial string Technologies { get; set; } = string.Empty;

    protected override Task<Error?> SaveCoreAsync(CancellationToken cancellationToken)
    {
        if (StartDate is not { } start)
        {
            return Task.FromResult<Error?>(Error.Validation("cv_start_date_required", "Start date is required."));
        }

        if (!IsOngoing && EndDate is null)
        {
            return Task.FromResult<Error?>(Error.Validation("cv_end_date_required", "End date is required unless this is ongoing."));
        }

        DateOnly? end = IsOngoing ? null : DateOnly.FromDateTime(EndDate!.Value);
        return _save(new PeriodDraft(_id, Organisation, Position, DateOnly.FromDateTime(start), end, Description, TechnologyTags.Split(Technologies)), cancellationToken);
    }

    protected override Task<Error?> DeleteCoreAsync(CancellationToken cancellationToken) =>
        _delete is null ? Task.FromResult<Error?>(null) : _delete(cancellationToken);
}

internal sealed record PeriodPrefill(Guid Id, string Organisation, string Position, DateOnly StartDate, DateOnly? EndDate, string? Description, IReadOnlyList<string> Technologies);

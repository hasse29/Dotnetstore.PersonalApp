using CommunityToolkit.Mvvm.ComponentModel;
using Dotnetstore.PersonalApp.Modules.Cvs.Domain;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Results;

namespace Dotnetstore.PersonalApp.Modules.Cvs.Features.Cvs;

internal sealed record LanguageDraft(Guid? Id, string Language, LanguageLevel Level);

internal sealed record LanguageLevelOption(LanguageLevel Level, string Text);

internal sealed partial class LanguageEditorViewModel : CvSectionEditorViewModel
{
    private readonly Guid? _id;
    private readonly Func<LanguageDraft, CancellationToken, Task<Error?>> _save;
    private readonly Func<CancellationToken, Task<Error?>>? _delete;

    public LanguageEditorViewModel(
        LanguageSkillDto? existing,
        Func<LanguageDraft, CancellationToken, Task<Error?>> save,
        Func<CancellationToken, Task<Error?>>? delete,
        ILocalizationService localization)
        : base(localization)
    {
        _id = existing?.Id;
        _save = save;
        _delete = delete;

        Levels = [.. Enum.GetValues<LanguageLevel>().Select(l => new LanguageLevelOption(l, LevelText(localization, l)))];
        SelectedLevel = Levels.First(l => l.Level == (existing?.Level ?? LanguageLevel.Good));
        Language = existing?.Language ?? string.Empty;
    }

    public override string Title => Localization.Get(_id is null ? "CvLanguageEditor_New" : "CvLanguageEditor_Edit");

    public override bool CanDelete => _id is not null;

    public IReadOnlyList<LanguageLevelOption> Levels { get; }

    [ObservableProperty]
    public partial string Language { get; set; } = string.Empty;

    [ObservableProperty]
    public partial LanguageLevelOption SelectedLevel { get; set; }

    public static string LevelText(ILocalizationService localization, LanguageLevel level) =>
        localization.Get($"CvLevel_{level}");

    protected override Task<Error?> SaveCoreAsync(CancellationToken cancellationToken) =>
        _save(new LanguageDraft(_id, Language, SelectedLevel.Level), cancellationToken);

    protected override Task<Error?> DeleteCoreAsync(CancellationToken cancellationToken) =>
        _delete is null ? Task.FromResult<Error?>(null) : _delete(cancellationToken);
}

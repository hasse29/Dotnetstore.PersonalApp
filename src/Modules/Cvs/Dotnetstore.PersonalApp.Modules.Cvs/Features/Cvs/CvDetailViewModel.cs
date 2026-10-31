using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dotnetstore.PersonalApp.Shared.Contacts;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;
using Dotnetstore.PersonalApp.Shared.Results;

namespace Dotnetstore.PersonalApp.Modules.Cvs.Features.Cvs;

/// <summary>
/// The overview of one CV: its owner's details and photo, the summary, work experience, education, courses, languages and
/// driver licence. The page only displays; each entry is changed in an editor that takes the page's place until it is closed.
/// </summary>
internal sealed partial class CvDetailViewModel : ViewModelBase
{
    private readonly CvDto _cv;
    private readonly Guid _userId;
    private readonly CvDetailServices _services;

    public CvDetailViewModel(CvDto cv, Guid userId, CvDetailServices services)
    {
        _cv = cv;
        _userId = userId;
        _services = services;

        var localization = services.Localization;
        Work = new CvSectionListViewModel(localization.Get("CvSection_Work"), localization.Get("CvSection_AddWork"), localization.Get("CvSection_Empty"), AddWork);
        Education = new CvSectionListViewModel(localization.Get("CvSection_Education"), localization.Get("CvSection_AddEducation"), localization.Get("CvSection_Empty"), AddEducation);
        Courses = new CvSectionListViewModel(localization.Get("CvSection_Courses"), localization.Get("CvSection_AddCourse"), localization.Get("CvSection_Empty"), AddCourse);
        Languages = new CvSectionListViewModel(localization.Get("CvSection_Languages"), localization.Get("CvSection_AddLanguage"), localization.Get("CvSection_Empty"), AddLanguage);
        Sections = [Work, Education, Courses, Languages];

        Title = cv.Title;
        TargetJob = cv.TargetJob;
        Summary = cv.Summary;
    }

    public event EventHandler? Closed;

    public CvSectionListViewModel Work { get; }

    public CvSectionListViewModel Education { get; }

    public CvSectionListViewModel Courses { get; }

    public CvSectionListViewModel Languages { get; }

    /// <summary>The section cards in display order.</summary>
    public IReadOnlyList<CvSectionListViewModel> Sections { get; }

    /// <summary>Every technology used in any job on this CV, most used first.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasNoTechnologies))]
    public partial IReadOnlyList<TechnologyChip> Technologies { get; set; } = [];

    public bool HasNoTechnologies => Technologies.Count == 0;

    [ObservableProperty]
    public partial string Title { get; set; }

    [ObservableProperty]
    public partial string? TargetJob { get; set; }

    [ObservableProperty]
    public partial string? Summary { get; set; }

    [ObservableProperty]
    public partial string FullName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Username { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string DateOfBirth { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Initials { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasNoPhoto))]
    [NotifyCanExecuteChangedFor(nameof(RemovePhotoCommand))]
    public partial byte[]? Photo { get; set; }

    [ObservableProperty]
    public partial string DriverLicence { get; set; } = string.Empty;

    /// <summary>The chosen contact entries as "Type: value" lines, followed by the LinkedIn link.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasNoContact))]
    public partial IReadOnlyList<string> ContactLines { get; set; } = [];

    public bool HasNoContact => ContactLines.Count == 0;

    [ObservableProperty]
    public partial CvSectionEditorViewModel? Editor { get; set; }

    [ObservableProperty]
    public partial string? StatusMessage { get; set; }

    public bool HasNoPhoto => Photo is null;

    private IReadOnlyList<string> _licence = [];
    private IReadOnlyList<ContactEntry> _availableContacts = [];
    private IReadOnlyList<Guid> _selectedContactIds = [];

    [RelayCommand]
    private void Back() => Closed?.Invoke(this, EventArgs.Empty);

    [RelayCommand]
    public async Task LoadAsync(CancellationToken cancellationToken)
    {
        var result = await _services.Get.HandleAsync(new GetCvDetail.Query(_cv.Id, _userId), cancellationToken);
        if (!result.IsSuccess)
        {
            StatusMessage = _services.Localization.Describe(result.Error);
            return;
        }

        var detail = result.Value;
        var localization = _services.Localization;
        var present = localization.Get("CvDetail_Present");

        Title = detail.Cv.Title;
        TargetJob = detail.Cv.TargetJob;
        Summary = detail.Cv.Summary;
        Photo = detail.Photo;

        if (detail.User is { } user)
        {
            FullName = user.FullName;
            Username = user.Username;
            DateOfBirth = user.DateOfBirth.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            Initials = string.Concat(
                user.FullName.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Take(2)
                    .Select(part => char.ToUpperInvariant(part[0])));
        }
        else
        {
            FullName = localization.Get("CvDetail_UserMissing");
            Username = string.Empty;
            DateOfBirth = string.Empty;
            Initials = "?";
        }

        _availableContacts = detail.AvailableContacts;
        _selectedContactIds = detail.SelectedContactIds;
        ContactLines =
        [
            .. detail.SelectedContacts.Select(c => $"{c.TypeName}: {c.Value}"),
            .. detail.Cv.LinkedIn is { } linkedIn ? (string[])[$"LinkedIn: {linkedIn}"] : [],
        ];

        _licence = detail.DriverLicence;
        DriverLicence = detail.DriverLicence.Count == 0 ? localization.Get("CvDetail_NoDriverLicence") : string.Join(", ", detail.DriverLicence);

        Work.SetItems(detail.WorkExperiences.Select(w => new CvSectionItem(
            w.Role, w.Employer, PeriodFormat.Format(w.StartDate, w.EndDate, present), w.Description, EditCommandFor(() => EditWork(w)), w.Technologies)));
        Technologies = [.. detail.WorkExperiences
            .SelectMany(w => w.Technologies)
            .GroupBy(t => t, StringComparer.OrdinalIgnoreCase)
            .Select(g => new TechnologyChip(g.First(), g.Count()))
            .OrderByDescending(c => c.Jobs)
            .ThenBy(c => c.Name, StringComparer.OrdinalIgnoreCase)];
        Education.SetItems(detail.Educations.Select(e => new CvSectionItem(
            e.Programme, e.School, PeriodFormat.Format(e.StartDate, e.EndDate, present), e.Description, EditCommandFor(() => EditEducation(e)))));
        Courses.SetItems(detail.Courses.Select(c => new CvSectionItem(
            c.Name, c.Provider, c.CompletedYear?.ToString(CultureInfo.InvariantCulture), null, EditCommandFor(() => EditCourse(c)))));
        Languages.SetItems(detail.Languages.Select(l => new CvSectionItem(
            l.Language, null, LanguageEditorViewModel.LevelText(localization, l.Level), null, EditCommandFor(() => EditLanguage(l)))));
    }

    [RelayCommand]
    private async Task ExportAsync(CancellationToken cancellationToken)
    {
        var result = await _services.Export.HandleAsync(new ExportCvToWord.Query(_cv.Id, _userId), cancellationToken);
        if (!result.IsSuccess)
        {
            StatusMessage = _services.Localization.Describe(result.Error);
            return;
        }

        StatusMessage = null;
        await _services.FileSave.SaveAsync(_services.Localization.Get("CvDetail_ExportTitle"), result.Value.FileName, result.Value.Content, cancellationToken);
    }

    [RelayCommand]
    private void EditContact() =>
        OpenEditor(new ContactEditorViewModel(
            _availableContacts,
            _selectedContactIds,
            async (ids, ct) => ErrorOf(await _services.SetContacts.HandleAsync(new SetCvContacts.Command(_cv.Id, _userId, ids), ct)),
            _services.Localization));

    [RelayCommand]
    private void EditDriverLicence() =>
        OpenEditor(new DriverLicenceEditorViewModel(
            _licence,
            async (categories, ct) => ErrorOf(await _services.SetDriverLicence.HandleAsync(new SetDriverLicence.Command(_cv.Id, _userId, categories), ct)),
            _services.Localization));

    [RelayCommand]
    private async Task ChoosePhotoAsync(CancellationToken cancellationToken)
    {
        var file = await _services.FileOpen.PickImageAsync(_services.Localization.Get("CvDetail_ChoosePhotoTitle"), SetCvPhoto.MaxPhotoBytes, cancellationToken);
        if (file is null)
        {
            StatusMessage = _services.Localization.Get("CvDetail_PhotoNotChanged");
            return;
        }

        await SetPhotoAsync(file.Content.ToArray(), cancellationToken);
    }

    [RelayCommand(CanExecute = nameof(HasPhoto))]
    private async Task RemovePhotoAsync(CancellationToken cancellationToken) => await SetPhotoAsync(null, cancellationToken);

    private bool HasPhoto() => Photo is not null;

    private async Task SetPhotoAsync(byte[]? photo, CancellationToken cancellationToken)
    {
        var result = await _services.SetPhoto.HandleAsync(new SetCvPhoto.Command(_cv.Id, _userId, photo), cancellationToken);
        await LoadAsync(cancellationToken);
        StatusMessage = result.IsSuccess ? null : _services.Localization.Describe(result.Error);
    }

    private void AddWork() => OpenEditor(WorkEditor(null));

    private void EditWork(WorkExperienceDto w) => OpenEditor(WorkEditor(w));

    private void AddEducation() => OpenEditor(EducationEditor(null));

    private void EditEducation(EducationDto e) => OpenEditor(EducationEditor(e));

    private void AddCourse() => OpenEditor(CourseEditor(null));

    private void EditCourse(CourseDto c) => OpenEditor(CourseEditor(c));

    private void AddLanguage() => OpenEditor(LanguageEditor(null));

    private void EditLanguage(LanguageSkillDto l) => OpenEditor(LanguageEditor(l));

    private PeriodEditorViewModel WorkEditor(WorkExperienceDto? existing) => new(
        PeriodKind.Work,
        existing is null ? null : new PeriodPrefill(existing.Id, existing.Employer, existing.Role, existing.StartDate, existing.EndDate, existing.Description, existing.Technologies),
        async (d, ct) => ErrorOf(await _services.SaveWork.HandleAsync(
            new SaveWorkExperience.Command(_cv.Id, _userId, d.Id, d.Organisation, d.Position, d.StartDate, d.EndDate, d.Description, d.Technologies), ct)),
        existing is null ? null : ct => DeleteAsync(CvSection.WorkExperience, existing.Id, ct),
        _services.Localization);

    private PeriodEditorViewModel EducationEditor(EducationDto? existing) => new(
        PeriodKind.Education,
        existing is null ? null : new PeriodPrefill(existing.Id, existing.School, existing.Programme, existing.StartDate, existing.EndDate, existing.Description, []),
        async (d, ct) => ErrorOf(await _services.SaveEducation.HandleAsync(
            new SaveEducation.Command(_cv.Id, _userId, d.Id, d.Organisation, d.Position, d.StartDate, d.EndDate, d.Description), ct)),
        existing is null ? null : ct => DeleteAsync(CvSection.Education, existing.Id, ct),
        _services.Localization);

    private CourseEditorViewModel CourseEditor(CourseDto? existing) => new(
        existing,
        async (d, ct) => ErrorOf(await _services.SaveCourse.HandleAsync(new SaveCourse.Command(_cv.Id, _userId, d.Id, d.Name, d.Provider, d.CompletedYear), ct)),
        existing is null ? null : ct => DeleteAsync(CvSection.Course, existing.Id, ct),
        _services.Localization);

    private LanguageEditorViewModel LanguageEditor(LanguageSkillDto? existing) => new(
        existing,
        async (d, ct) => ErrorOf(await _services.SaveLanguage.HandleAsync(new SaveLanguageSkill.Command(_cv.Id, _userId, d.Id, d.Language, d.Level), ct)),
        existing is null ? null : ct => DeleteAsync(CvSection.Language, existing.Id, ct),
        _services.Localization);

    private async Task<Error?> DeleteAsync(CvSection section, Guid itemId, CancellationToken cancellationToken) =>
        ErrorOf(await _services.DeleteItem.HandleAsync(new DeleteCvSectionItem.Command(_cv.Id, _userId, section, itemId), cancellationToken));

    private static Error? ErrorOf<T>(Result<T> result) => result.IsSuccess ? null : result.Error;

    private static RelayCommand EditCommandFor(Action edit) => new(edit);

    private void OpenEditor(CvSectionEditorViewModel editor)
    {
        CloseEditor();
        StatusMessage = null;
        Editor = editor;
        editor.Saved += OnEditorFinished;
        editor.Deleted += OnEditorFinished;
        editor.Cancelled += OnEditorCancelled;
    }

    private void CloseEditor()
    {
        if (Editor is { } editor)
        {
            editor.Saved -= OnEditorFinished;
            editor.Deleted -= OnEditorFinished;
            editor.Cancelled -= OnEditorCancelled;
        }

        Editor = null;
    }

    // Event handler, so it has to be async void; LoadAsync reports a failure on the status line.
    private async void OnEditorFinished(object? sender, EventArgs e)
    {
        CloseEditor();
        await LoadAsync(CancellationToken.None);
    }

    private void OnEditorCancelled(object? sender, EventArgs e) => CloseEditor();
}

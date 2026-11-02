using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dotnetstore.PersonalApp.Modules.Subjects.Features.Subjects;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;

namespace Dotnetstore.PersonalApp.Modules.Subjects.Features.TeacherSubjects;

/// <summary>The Subjects tab of a teacher: the subjects they teach, with a picker to add one from the catalogue.</summary>
internal sealed partial class UserSubjectsViewModel(
    Guid userId,
    ListTeacherSubjects.Handler listTaught,
    ListSubjects.Handler listCatalogue,
    AddTeacherSubject.Handler add,
    RemoveTeacherSubject.Handler remove,
    ILocalizationService localization) : ViewModelBase
{
    public ObservableCollection<SubjectDto> Taught { get; } = [];

    /// <summary>The catalogue subjects the teacher does not teach yet.</summary>
    public ObservableCollection<SubjectDto> Available { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddCommand))]
    public partial SubjectDto? SelectedAvailable { get; set; }

    [ObservableProperty]
    public partial string? StatusMessage { get; set; }

    [ObservableProperty]
    public partial bool HasCatalogue { get; set; }

    [RelayCommand]
    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        var taught = await listTaught.HandleAsync(new ListTeacherSubjects.Query(userId), cancellationToken);
        var catalogue = await listCatalogue.HandleAsync(new ListSubjects.Query(PageSize: ListSubjects.MaxPageSize), cancellationToken);

        Taught.Clear();
        foreach (var subject in taught)
        {
            Taught.Add(subject);
        }

        var taughtIds = taught.Select(s => s.Id).ToHashSet();
        Available.Clear();
        foreach (var subject in catalogue.Items.Where(s => !taughtIds.Contains(s.Id)))
        {
            Available.Add(subject);
        }

        SelectedAvailable = null;
        HasCatalogue = catalogue.TotalCount > 0;
        StatusMessage = localization.Format("UserSubjects_Count", taught.Count);
    }

    [RelayCommand(CanExecute = nameof(CanAdd))]
    private async Task AddAsync(CancellationToken cancellationToken)
    {
        if (SelectedAvailable is not { } subject)
        {
            return;
        }

        var result = await add.HandleAsync(new AddTeacherSubject.Command(userId, subject.Id), cancellationToken);

        await LoadAsync(cancellationToken);
        if (!result.IsSuccess)
        {
            StatusMessage = localization.Describe(result.Error);
        }
    }

    private bool CanAdd() => SelectedAvailable is not null;

    [RelayCommand]
    private async Task RemoveAsync(SubjectDto subject, CancellationToken cancellationToken)
    {
        var result = await remove.HandleAsync(new RemoveTeacherSubject.Command(userId, subject.Id), cancellationToken);

        await LoadAsync(cancellationToken);
        if (!result.IsSuccess)
        {
            StatusMessage = localization.Describe(result.Error);
        }
    }
}

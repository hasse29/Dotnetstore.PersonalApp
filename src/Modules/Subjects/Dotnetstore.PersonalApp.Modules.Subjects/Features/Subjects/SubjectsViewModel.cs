using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;

namespace Dotnetstore.PersonalApp.Modules.Subjects.Features.Subjects;

internal sealed partial class SubjectsViewModel(
    ListSubjects.Handler list,
    CreateSubject.Handler create,
    UpdateSubject.Handler update,
    DeleteSubject.Handler delete,
    ILocalizationService localization) : ViewModelBase
{
    public ObservableCollection<SubjectDto> Subjects { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RequestDeleteCommand))]
    [NotifyCanExecuteChangedFor(nameof(ConfirmDeleteCommand))]
    public partial SubjectDto? SelectedSubject { get; set; }

    [ObservableProperty]
    public partial SubjectEditorViewModel? Editor { get; set; }

    [ObservableProperty]
    public partial string Search { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string? StatusMessage { get; set; }

    [ObservableProperty]
    public partial bool IsConfirmingDelete { get; set; }

    partial void OnSelectedSubjectChanged(SubjectDto? value)
    {
        IsConfirmingDelete = false;
        if (value is not null)
        {
            OpenEditor(new SubjectEditorViewModel(value, create, update, localization));
        }
    }

    [RelayCommand]
    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        var response = await list.HandleAsync(new ListSubjects.Query(Search), cancellationToken);

        Subjects.Clear();
        foreach (var subject in response.Items)
        {
            Subjects.Add(subject);
        }

        StatusMessage = response.TotalCount > response.Items.Count
            ? localization.Format("Subjects_ShowingPartial", response.Items.Count, response.TotalCount)
            : localization.Format("Subjects_Count", response.TotalCount);
    }

    [RelayCommand]
    private void New()
    {
        SelectedSubject = null;
        OpenEditor(new SubjectEditorViewModel(null, create, update, localization));
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void RequestDelete() => IsConfirmingDelete = true;

    [RelayCommand]
    private void CancelDelete() => IsConfirmingDelete = false;

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private async Task ConfirmDeleteAsync(CancellationToken cancellationToken)
    {
        if (SelectedSubject is not { } subject)
        {
            return;
        }

        var result = await delete.HandleAsync(new DeleteSubject.Command(subject.Id), cancellationToken);

        CloseEditor();
        await LoadAsync(cancellationToken);
        if (!result.IsSuccess)
        {
            StatusMessage = localization.Describe(result.Error);
        }
    }

    private bool HasSelection() => SelectedSubject is not null;

    private void OpenEditor(SubjectEditorViewModel editor)
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
        SelectedSubject = null;
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
    private async void OnEditorSaved(object? sender, SubjectDto saved)
    {
        CloseEditor();
        await LoadAsync(CancellationToken.None);
        StatusMessage = localization.Format("Subjects_Saved", saved.Name);
    }

    private void OnEditorCancelled(object? sender, EventArgs e) => CloseEditor();
}

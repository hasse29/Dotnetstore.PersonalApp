using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;

namespace Dotnetstore.PersonalApp.Modules.Grades.Features.Scales;

internal sealed partial class GradeScalesViewModel(
    ListGradeScales.Handler list,
    CreateGradeScale.Handler create,
    UpdateGradeScale.Handler update,
    DeleteGradeScale.Handler delete,
    ILocalizationService localization) : ViewModelBase
{
    public ObservableCollection<GradeScaleDto> Scales { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RequestDeleteCommand))]
    [NotifyCanExecuteChangedFor(nameof(ConfirmDeleteCommand))]
    public partial GradeScaleDto? SelectedScale { get; set; }

    [ObservableProperty]
    public partial GradeScaleEditorViewModel? Editor { get; set; }

    [ObservableProperty]
    public partial string? StatusMessage { get; set; }

    [ObservableProperty]
    public partial bool IsConfirmingDelete { get; set; }

    partial void OnSelectedScaleChanged(GradeScaleDto? value)
    {
        IsConfirmingDelete = false;
        if (value is not null)
        {
            OpenEditor(new GradeScaleEditorViewModel(value, create, update, localization));
        }
    }

    [RelayCommand]
    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        var scales = await list.HandleAsync(new ListGradeScales.Query(), cancellationToken);

        Scales.Clear();
        foreach (var scale in scales)
        {
            Scales.Add(scale);
        }

        StatusMessage = localization.Format("GradeScales_Count", scales.Count);
    }

    [RelayCommand]
    private void New()
    {
        SelectedScale = null;
        OpenEditor(new GradeScaleEditorViewModel(null, create, update, localization));
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void RequestDelete() => IsConfirmingDelete = true;

    [RelayCommand]
    private void CancelDelete() => IsConfirmingDelete = false;

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private async Task ConfirmDeleteAsync(CancellationToken cancellationToken)
    {
        if (SelectedScale is not { } scale)
        {
            return;
        }

        var result = await delete.HandleAsync(new DeleteGradeScale.Command(scale.Id), cancellationToken);

        CloseEditor();
        await LoadAsync(cancellationToken);
        if (!result.IsSuccess)
        {
            StatusMessage = localization.Describe(result.Error);
        }
    }

    private bool HasSelection() => SelectedScale is not null;

    private void OpenEditor(GradeScaleEditorViewModel editor)
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
        SelectedScale = null;
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
    private async void OnEditorSaved(object? sender, GradeScaleDto saved)
    {
        CloseEditor();
        await LoadAsync(CancellationToken.None);
        StatusMessage = localization.Format("GradeScales_Saved", saved.Name);
    }

    private void OnEditorCancelled(object? sender, EventArgs e) => CloseEditor();
}

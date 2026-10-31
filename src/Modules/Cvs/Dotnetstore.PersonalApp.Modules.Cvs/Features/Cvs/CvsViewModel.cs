using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;

namespace Dotnetstore.PersonalApp.Modules.Cvs.Features.Cvs;

/// <summary>
/// The CVs of one user: on the module's own page for the signed-in user, or as a tab on a user's detail page
/// (<paramref name="isPageLayout"/> false).
/// </summary>
internal sealed partial class CvsViewModel(
    Guid userId,
    bool isPageLayout,
    ListCvs.Handler list,
    CreateCv.Handler create,
    UpdateCv.Handler update,
    DeleteCv.Handler delete,
    CvDetailServices detailServices,
    ILocalizationService localization) : ViewModelBase
{
    public ObservableCollection<CvDto> Cvs { get; } = [];

    /// <summary>True on the module's own page, false when shown as a tab on a user's detail page.</summary>
    public bool IsPageLayout => isPageLayout;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(OpenDetailCommand))]
    [NotifyCanExecuteChangedFor(nameof(RequestDeleteCommand))]
    [NotifyCanExecuteChangedFor(nameof(ConfirmDeleteCommand))]
    public partial CvDto? SelectedCv { get; set; }

    [ObservableProperty]
    public partial CvEditorViewModel? Editor { get; set; }

    [ObservableProperty]
    public partial string? StatusMessage { get; set; }

    [ObservableProperty]
    public partial bool IsConfirmingDelete { get; set; }

    [ObservableProperty]
    public partial CvDetailViewModel? Detail { get; set; }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void OpenDetail()
    {
        if (SelectedCv is not { } cv)
        {
            return;
        }

        var detail = new CvDetailViewModel(cv, userId, detailServices);
        detail.Closed += OnDetailClosed;
        Detail = detail;
    }

    private async void OnDetailClosed(object? sender, EventArgs e)
    {
        if (sender is CvDetailViewModel detail)
        {
            detail.Closed -= OnDetailClosed;
        }

        Detail = null;
        await LoadAsync(CancellationToken.None);
    }

    partial void OnSelectedCvChanged(CvDto? value)
    {
        IsConfirmingDelete = false;
        if (value is not null)
        {
            OpenEditor(new CvEditorViewModel(value, userId, create, update, localization));
        }
    }

    [RelayCommand]
    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        var response = await list.HandleAsync(new ListCvs.Query(userId), cancellationToken);

        Cvs.Clear();
        foreach (var cv in response.Items)
        {
            Cvs.Add(cv);
        }

        StatusMessage = response.TotalCount > response.Items.Count
            ? localization.Format("Cvs_ShowingPartial", response.Items.Count, response.TotalCount)
            : localization.Format("Cvs_Count", response.TotalCount);
    }

    [RelayCommand]
    private void New()
    {
        SelectedCv = null;
        OpenEditor(new CvEditorViewModel(null, userId, create, update, localization));
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void RequestDelete() => IsConfirmingDelete = true;

    [RelayCommand]
    private void CancelDelete() => IsConfirmingDelete = false;

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private async Task ConfirmDeleteAsync(CancellationToken cancellationToken)
    {
        if (SelectedCv is not { } cv)
        {
            return;
        }

        var result = await delete.HandleAsync(new DeleteCv.Command(cv.Id, userId), cancellationToken);

        CloseEditor();
        await LoadAsync(cancellationToken);
        if (!result.IsSuccess)
        {
            StatusMessage = localization.Describe(result.Error);
        }
    }

    private bool HasSelection() => SelectedCv is not null;

    private void OpenEditor(CvEditorViewModel editor)
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
        SelectedCv = null;
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
    private async void OnEditorSaved(object? sender, CvDto saved)
    {
        CloseEditor();
        await LoadAsync(CancellationToken.None);
        StatusMessage = localization.Format("Cvs_Saved", saved.Title);
    }

    private void OnEditorCancelled(object? sender, EventArgs e) => CloseEditor();
}

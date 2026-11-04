using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dotnetstore.PersonalApp.Modules.Production.Features;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;

namespace Dotnetstore.PersonalApp.Modules.Production.Features.ScrapReasons;

internal sealed partial class ScrapReasonsViewModel(
    ListScrapReasons.Handler list,
    CreateScrapReason.Handler create,
    UpdateScrapReason.Handler update,
    DeleteScrapReason.Handler delete,
    TimeProvider clock,
    ILocalizationService localization) : ViewModelBase
{
    public ObservableCollection<ScrapReasonDto> ScrapReasons { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RequestDeleteCommand))]
    [NotifyCanExecuteChangedFor(nameof(ConfirmDeleteCommand))]
    public partial ScrapReasonDto? SelectedScrapReason { get; set; }

    [ObservableProperty]
    public partial ScrapReasonEditorViewModel? Editor { get; set; }

    [ObservableProperty]
    public partial string Search { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string? StatusMessage { get; set; }

    [ObservableProperty]
    public partial bool IsConfirmingDelete { get; set; }

    partial void OnSelectedScrapReasonChanged(ScrapReasonDto? value)
    {
        IsConfirmingDelete = false;
        if (value is not null)
        {
            OpenEditor(new ScrapReasonEditorViewModel(value, create, update, clock, localization));
        }
    }

    [RelayCommand]
    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        var response = await list.HandleAsync(new ListScrapReasons.Query(Search), cancellationToken);

        ScrapReasons.Clear();
        foreach (var item in response.Items)
        {
            ScrapReasons.Add(item);
        }

        StatusMessage = response.TotalCount > response.Items.Count
            ? localization.Format("ScrapReasons_ShowingPartial", response.Items.Count, response.TotalCount)
            : localization.Format("ScrapReasons_Count", response.TotalCount);
    }

    [RelayCommand]
    private void New()
    {
        SelectedScrapReason = null;
        OpenEditor(new ScrapReasonEditorViewModel(null, create, update, clock, localization));
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void RequestDelete() => IsConfirmingDelete = true;

    [RelayCommand]
    private void CancelDelete() => IsConfirmingDelete = false;

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private async Task ConfirmDeleteAsync(CancellationToken cancellationToken)
    {
        if (SelectedScrapReason is not { } selected)
        {
            return;
        }

        var result = await delete.HandleAsync(new DeleteScrapReason.Command(selected.Id), cancellationToken);

        CloseEditor();
        await LoadAsync(cancellationToken);
        if (!result.IsSuccess)
        {
            StatusMessage = localization.Describe(result.Error);
        }
    }

    private bool HasSelection() => SelectedScrapReason is not null;

    private void OpenEditor(ScrapReasonEditorViewModel editor)
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
        SelectedScrapReason = null;
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
    private async void OnEditorSaved(object? sender, ScrapReasonDto saved)
    {
        CloseEditor();
        await LoadAsync(CancellationToken.None);
        StatusMessage = localization.Get("ScrapReasons_Saved");
    }

    private void OnEditorCancelled(object? sender, EventArgs e) => CloseEditor();
}
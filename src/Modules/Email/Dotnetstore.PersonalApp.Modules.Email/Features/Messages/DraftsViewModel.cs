using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;

namespace Dotnetstore.PersonalApp.Modules.Email.Features.Messages;

/// <summary>The user's saved drafts: pick one to continue writing it in an embedded composer, or delete it.</summary>
internal sealed partial class DraftsViewModel(
    Guid userId,
    ListDrafts.Handler listDrafts,
    GetDraft.Handler getDraft,
    DeleteDraft.Handler deleteDraft,
    GetEmailAttachment.Handler getAttachment,
    Func<ComposeViewModel> composeFactory,
    ILocalizationService localization) : ViewModelBase
{
    public ObservableCollection<DraftDto> Drafts { get; } = [];

    [ObservableProperty]
    public partial DraftDto? SelectedDraft { get; set; }

    /// <summary>The draft being edited, shown next to the list; null when none is open.</summary>
    [ObservableProperty]
    public partial ComposeViewModel? Composer { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RequestDeleteCommand))]
    [NotifyCanExecuteChangedFor(nameof(ConfirmDeleteCommand))]
    public partial bool HasSelection { get; set; }

    [ObservableProperty]
    public partial bool IsConfirmingDelete { get; set; }

    [ObservableProperty]
    public partial string? StatusMessage { get; set; }

    [ObservableProperty]
    public partial bool StatusIsError { get; set; }

    partial void OnSelectedDraftChanged(DraftDto? value)
    {
        IsConfirmingDelete = false;
        HasSelection = value is not null;
        _ = OpenAsync(value, CancellationToken.None);
    }

    [RelayCommand]
    private Task LoadAsync(CancellationToken cancellationToken) => ReloadAsync(cancellationToken);

    private async Task ReloadAsync(CancellationToken cancellationToken)
    {
        var keep = SelectedDraft?.Id;
        var response = await listDrafts.HandleAsync(new ListDrafts.Query(userId), cancellationToken);

        Drafts.Clear();
        foreach (var draft in response.Items)
        {
            Drafts.Add(draft);
        }

        // Restore the highlight; OpenAsync leaves an already open composer alone so a refresh never wipes what is being typed.
        SelectedDraft = Drafts.FirstOrDefault(d => d.Id == keep);

        StatusIsError = false;
        StatusMessage = localization.Format("Drafts_Count", response.TotalCount);
    }

    private async Task OpenAsync(DraftDto? summary, CancellationToken cancellationToken)
    {
        if (summary is null)
        {
            if (Composer is { DraftId: not null })
            {
                CloseComposer();
            }

            return;
        }

        if (Composer is { } open && open.DraftId == summary.Id)
        {
            return;
        }

        var result = await getDraft.HandleAsync(new GetDraft.Query(summary.Id, userId), cancellationToken);
        if (!result.IsSuccess)
        {
            StatusIsError = true;
            StatusMessage = localization.Describe(result.Error);
            return;
        }

        var attachments = new List<AttachmentInput>();
        foreach (var attachment in result.Value.Attachments)
        {
            var content = await getAttachment.HandleAsync(new GetEmailAttachment.Query(attachment.Id, userId), cancellationToken);
            if (content.IsSuccess)
            {
                attachments.Add(new AttachmentInput(content.Value.FileName, content.Value.Content));
            }
        }

        CloseComposer();
        var composer = composeFactory();
        composer.Sent += OnComposerFinished;
        composer.Cancelled += OnComposerCancelled;
        composer.DraftSaved += OnComposerDraftSaved;
        Composer = composer;
        await composer.LoadDraftAsync(result.Value, attachments, cancellationToken);
    }

    private void CloseComposer()
    {
        if (Composer is { } composer)
        {
            composer.Sent -= OnComposerFinished;
            composer.Cancelled -= OnComposerCancelled;
            composer.DraftSaved -= OnComposerDraftSaved;
            Composer = null;
        }
    }

    private void OnComposerFinished(object? sender, EventArgs e)
    {
        CloseComposer();
        SelectedDraft = null;
        _ = ReloadAsync(CancellationToken.None);
    }

    private void OnComposerCancelled(object? sender, EventArgs e)
    {
        CloseComposer();
        SelectedDraft = null;
    }

    private void OnComposerDraftSaved(object? sender, EventArgs e) => _ = ReloadAsync(CancellationToken.None);

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void RequestDelete() => IsConfirmingDelete = true;

    [RelayCommand]
    private void CancelDelete() => IsConfirmingDelete = false;

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private async Task ConfirmDeleteAsync(CancellationToken cancellationToken)
    {
        if (SelectedDraft is not { } draft)
        {
            return;
        }

        var result = await deleteDraft.HandleAsync(new DeleteDraft.Command(draft.Id, userId), cancellationToken);

        IsConfirmingDelete = false;
        CloseComposer();
        SelectedDraft = null;
        await ReloadAsync(cancellationToken);
        if (!result.IsSuccess)
        {
            StatusIsError = true;
            StatusMessage = localization.Describe(result.Error);
        }
    }
}

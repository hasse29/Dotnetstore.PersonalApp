using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dotnetstore.PersonalApp.Modules.Email.Domain;
using Dotnetstore.PersonalApp.Modules.Email.Features.EmailAccounts;
using Dotnetstore.PersonalApp.Modules.Email.Mail;
using Dotnetstore.PersonalApp.Shared.Files;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;

namespace Dotnetstore.PersonalApp.Modules.Email.Features.Messages;

/// <summary>
/// Writes and sends a message through one of the SMTP accounts the user can use, as text or HTML, with attachments.
/// On its own page it stays open for the next message; embedded in the inbox (for reply and forward) it closes when sent or cancelled.
/// </summary>
internal sealed partial class ComposeViewModel(
    Guid userId,
    bool isEmbedded,
    ListEmailAccounts.Handler listAccounts,
    SendEmail.Handler send,
    SaveDraft.Handler saveDraft,
    DeleteDraft.Handler deleteDraft,
    IFilePickService filePicker,
    ILocalizationService localization) : ViewModelBase
{
    public ObservableCollection<AccountChoice> Accounts { get; } = [];

    public ObservableCollection<ComposeAttachment> Attachments { get; } = [];

    /// <summary>True when shown inside the inbox: no page title or margin, and a Cancel button.</summary>
    public bool IsEmbedded => isEmbedded;

    public bool IsPage => !isEmbedded;

    /// <summary>Raised after a message was sent, so the inbox can close the composer.</summary>
    public event EventHandler? Sent;

    /// <summary>Raised when the user cancels an embedded composer.</summary>
    public event EventHandler? Cancelled;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SendCommand))]
    public partial AccountChoice? SelectedAccount { get; set; }

    [ObservableProperty]
    public partial string To { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Cc { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Subject { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Body { get; set; } = string.Empty;

    /// <summary>The saved draft this message came from or was saved as; null until it is saved. Sending deletes it.</summary>
    public Guid? DraftId { get; private set; }

    /// <summary>Raised after a draft was saved, so the drafts page can refresh.</summary>
    public event EventHandler? DraftSaved;

    [ObservableProperty]
    public partial bool IsHtml { get; set; }

    [ObservableProperty]
    public partial bool HasNoAccounts { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SendCommand))]
    public partial bool IsSending { get; set; }

    [ObservableProperty]
    public partial string? StatusMessage { get; set; }

    [ObservableProperty]
    public partial bool StatusIsError { get; set; }

    [RelayCommand]
    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        var response = await listAccounts.HandleAsync(new ListEmailAccounts.Query(userId), cancellationToken);

        var previous = SelectedAccount?.Id;
        Accounts.Clear();
        foreach (var account in response.Items.Where(a => a.Protocol == MailProtocol.Smtp))
        {
            Accounts.Add(AccountChoice.From(account));
        }

        HasNoAccounts = Accounts.Count == 0;
        SelectedAccount = Accounts.FirstOrDefault(a => a.Id == previous) ?? Accounts.FirstOrDefault();
    }

    /// <summary>Fills in a reply or a forward. The sending account is the one with <paramref name="preferredAddress"/> if there is one.</summary>
    public async Task PrepareAsync(
        string? preferredAddress,
        string to,
        string cc,
        string subject,
        string body,
        IEnumerable<AttachmentInput> attachments,
        CancellationToken cancellationToken)
    {
        await LoadAsync(cancellationToken);

        SelectedAccount = Accounts.FirstOrDefault(a => string.Equals(a.EmailAddress, preferredAddress, StringComparison.OrdinalIgnoreCase))
            ?? SelectedAccount;
        DraftId = null;
        To = to;
        Cc = cc;
        Subject = subject;
        Body = body;
        Attachments.Clear();
        foreach (var attachment in attachments)
        {
            AddAttachment(attachment.FileName, attachment.Content);
        }
    }

    /// <summary>Continues a saved draft: the form is filled from it and saving again replaces it.</summary>
    public async Task LoadDraftAsync(DraftDto draft, IEnumerable<AttachmentInput> attachments, CancellationToken cancellationToken)
    {
        await LoadAsync(cancellationToken);

        SelectedAccount = Accounts.FirstOrDefault(a => a.Id == draft.AccountId) ?? SelectedAccount;
        DraftId = draft.Id;
        To = draft.To;
        Cc = draft.Cc;
        Subject = draft.Subject;
        Body = draft.Body;
        IsHtml = draft.IsHtml;
        Attachments.Clear();
        foreach (var attachment in attachments)
        {
            AddAttachment(attachment.FileName, attachment.Content);
        }

        StatusMessage = null;
    }

    [RelayCommand]
    private async Task SaveDraftAsync(CancellationToken cancellationToken)
    {
        var attachments = Attachments.Select(a => new AttachmentInput(a.FileName, a.Content)).ToList();
        var result = await saveDraft.HandleAsync(
            new SaveDraft.Command(userId, DraftId, SelectedAccount?.Id, To, Cc, Subject, Body, IsHtml, attachments),
            cancellationToken);

        StatusIsError = !result.IsSuccess;
        if (result.IsSuccess)
        {
            DraftId = result.Value.Id;
            StatusMessage = localization.Get("Compose_DraftSaved");
            DraftSaved?.Invoke(this, EventArgs.Empty);
        }
        else
        {
            StatusMessage = localization.Describe(result.Error);
        }
    }

    [RelayCommand]
    private async Task AddAttachmentsAsync(CancellationToken cancellationToken)
    {
        var picked = await filePicker.PickFilesAsync(localization.Get("Compose_AttachTitle"), MailLimits.MaxAttachmentBytes, cancellationToken);
        foreach (var file in picked.Files)
        {
            AddAttachment(file.Name, file.Content.ToArray());
        }

        if (picked.Skipped.Count > 0)
        {
            StatusIsError = true;
            StatusMessage = localization.Format("Compose_FilesSkipped", string.Join(", ", picked.Skipped), SizeText.Format(MailLimits.MaxAttachmentBytes));
        }
    }

    private void AddAttachment(string fileName, byte[] content)
    {
        ComposeAttachment? added = null;
        added = new ComposeAttachment(fileName, content, SizeText.Format(content.Length), new RelayCommand(() => Attachments.Remove(added!)));
        Attachments.Add(added);
    }

    [RelayCommand(CanExecute = nameof(CanSend))]
    private async Task SendAsync(CancellationToken cancellationToken)
    {
        if (SelectedAccount is not { } account)
        {
            return;
        }

        IsSending = true;
        StatusIsError = false;
        StatusMessage = localization.Get("Compose_Sending");
        try
        {
            var attachments = Attachments.Select(a => new AttachmentInput(a.FileName, a.Content)).ToList();
            var result = await send.HandleAsync(new SendEmail.Command(userId, account.Id, To, Subject, Body, IsHtml, attachments, Cc), cancellationToken);
            if (result.IsSuccess)
            {
                StatusMessage = localization.Format("Compose_Sent", result.Value);
                if (DraftId is { } draftId)
                {
                    // The message is out; a leftover draft of it would only confuse. A failure here is not worth reporting.
                    await deleteDraft.HandleAsync(new DeleteDraft.Command(draftId, userId), CancellationToken.None);
                    DraftId = null;
                    DraftSaved?.Invoke(this, EventArgs.Empty);
                }

                To = string.Empty;
                Cc = string.Empty;
                Subject = string.Empty;
                Body = string.Empty;
                IsHtml = false;
                Attachments.Clear();
                Sent?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                StatusIsError = true;
                var text = localization.Describe(result.Error);
                StatusMessage = MailGatewayException.IsGatewayCode(result.Error.Code) && !string.IsNullOrWhiteSpace(result.Error.Message)
                    ? $"{text} ({result.Error.Message})"
                    : text;
            }
        }
        finally
        {
            IsSending = false;
        }
    }

    private bool CanSend() => SelectedAccount is not null && !IsSending;

    [RelayCommand]
    private void Cancel() => Cancelled?.Invoke(this, EventArgs.Empty);
}

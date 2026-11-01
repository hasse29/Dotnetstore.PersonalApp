using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dotnetstore.PersonalApp.Modules.Email.Domain;
using Dotnetstore.PersonalApp.Modules.Email.Features.EmailAccounts;
using Dotnetstore.PersonalApp.Shared.Files;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;

namespace Dotnetstore.PersonalApp.Modules.Email.Features.Messages;

/// <summary>The copies of the messages sent through one SMTP account: read, save attachments, delete.</summary>
internal sealed partial class SentViewModel(
    Guid userId,
    ListEmailAccounts.Handler listAccounts,
    ListSentMessages.Handler listMessages,
    GetSentMessage.Handler getMessage,
    DeleteSentMessage.Handler deleteMessage,
    GetEmailAttachment.Handler getAttachment,
    IFileSaveService fileSave,
    ILocalizationService localization) : ViewModelBase
{
    public ObservableCollection<AccountChoice> Accounts { get; } = [];

    public ObservableCollection<SentMessageDto> Messages { get; } = [];

    public ObservableCollection<AttachmentRow> AttachmentRows { get; } = [];

    [ObservableProperty]
    public partial AccountChoice? SelectedAccount { get; set; }

    [ObservableProperty]
    public partial SentMessageDto? SelectedMessage { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RequestDeleteCommand))]
    [NotifyCanExecuteChangedFor(nameof(ConfirmDeleteCommand))]
    public partial SentMessageDto? OpenMessage { get; set; }

    /// <summary>The open message's HTML as blocks; empty for plain text messages.</summary>
    [ObservableProperty]
    public partial IReadOnlyList<HtmlBlock> FormattedBlocks { get; set; } = [];

    [ObservableProperty]
    public partial bool HasHtml { get; set; }

    [ObservableProperty]
    public partial bool IsConfirmingDelete { get; set; }

    [ObservableProperty]
    public partial bool HasNoAccounts { get; set; }

    [ObservableProperty]
    public partial string? StatusMessage { get; set; }

    [ObservableProperty]
    public partial bool StatusIsError { get; set; }

    partial void OnSelectedAccountChanged(AccountChoice? value)
    {
        OpenMessage = null;
        _ = ReloadAsync(CancellationToken.None);
    }

    partial void OnSelectedMessageChanged(SentMessageDto? value)
    {
        IsConfirmingDelete = false;
        _ = OpenAsync(value, CancellationToken.None);
    }

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

    private async Task ReloadAsync(CancellationToken cancellationToken)
    {
        Messages.Clear();
        if (SelectedAccount is not { } account)
        {
            return;
        }

        var result = await listMessages.HandleAsync(new ListSentMessages.Query(userId, account.Id), cancellationToken);
        if (!result.IsSuccess)
        {
            StatusIsError = true;
            StatusMessage = localization.Describe(result.Error);
            return;
        }

        foreach (var message in result.Value.Items)
        {
            Messages.Add(message);
        }

        StatusIsError = false;
        StatusMessage = localization.Format("Sent_Count", result.Value.TotalCount);
    }

    private async Task OpenAsync(SentMessageDto? summary, CancellationToken cancellationToken)
    {
        AttachmentRows.Clear();
        FormattedBlocks = [];
        HasHtml = false;
        if (summary is null)
        {
            OpenMessage = null;
            return;
        }

        var result = await getMessage.HandleAsync(new GetSentMessage.Query(summary.Id, userId), cancellationToken);
        OpenMessage = result.IsSuccess ? result.Value : null;
        if (result.IsSuccess)
        {
            if (result.Value.IsHtml)
            {
                FormattedBlocks = HtmlFormatter.Format(result.Value.Body);
                HasHtml = FormattedBlocks.Count > 0;
            }

            foreach (var attachment in result.Value.Attachments)
            {
                AttachmentRows.Add(new AttachmentRow(
                    $"{attachment.FileName} ({SizeText.Format(attachment.Size)})",
                    new AsyncRelayCommand(ct => SaveAttachmentAsync(attachment, ct))));
            }
        }
    }

    private async Task SaveAttachmentAsync(EmailAttachmentDto attachment, CancellationToken cancellationToken)
    {
        var result = await getAttachment.HandleAsync(new GetEmailAttachment.Query(attachment.Id, userId), cancellationToken);
        if (!result.IsSuccess)
        {
            StatusIsError = true;
            StatusMessage = localization.Describe(result.Error);
            return;
        }

        var saved = await fileSave.SaveAsync(localization.Get("Inbox_SaveAttachment"), result.Value.FileName, result.Value.Content, cancellationToken);
        if (saved is not null)
        {
            StatusIsError = false;
            StatusMessage = localization.Format("Inbox_AttachmentSaved", result.Value.FileName);
        }
    }

    [RelayCommand(CanExecute = nameof(HasOpenMessage))]
    private void RequestDelete() => IsConfirmingDelete = true;

    [RelayCommand]
    private void CancelDelete() => IsConfirmingDelete = false;

    [RelayCommand(CanExecute = nameof(HasOpenMessage))]
    private async Task ConfirmDeleteAsync(CancellationToken cancellationToken)
    {
        if (OpenMessage is not { } message)
        {
            return;
        }

        var result = await deleteMessage.HandleAsync(new DeleteSentMessage.Command(message.Id, userId), cancellationToken);

        IsConfirmingDelete = false;
        SelectedMessage = null;
        await ReloadAsync(cancellationToken);
        if (!result.IsSuccess)
        {
            StatusIsError = true;
            StatusMessage = localization.Describe(result.Error);
        }
    }

    private bool HasOpenMessage() => OpenMessage is not null;
}

using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dotnetstore.PersonalApp.Modules.Email.Domain;
using Dotnetstore.PersonalApp.Modules.Email.Features.EmailAccounts;
using Dotnetstore.PersonalApp.Modules.Email.Mail;
using Dotnetstore.PersonalApp.Shared.Files;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;
using Dotnetstore.PersonalApp.Shared.Results;

namespace Dotnetstore.PersonalApp.Modules.Email.Features.Messages;

/// <summary>
/// The downloaded mail of one POP3 account: fetch (by hand or on a timer), read, save attachments, delete locally, reply and forward.
/// The view runs a timer that calls <see cref="AutoRefreshCommand"/> every <see cref="AutoRefreshMinutes"/> minutes.
/// </summary>
internal sealed partial class InboxViewModel(
    Guid userId,
    ListEmailAccounts.Handler listAccounts,
    FetchEmail.Handler fetch,
    ListEmailMessages.Handler listMessages,
    GetEmailMessage.Handler getMessage,
    DeleteEmailMessage.Handler deleteMessage,
    GetEmailAttachment.Handler getAttachment,
    IFileSaveService fileSave,
    Func<ComposeViewModel> composeFactory,
    ILocalizationService localization) : ViewModelBase
{
    public const int AutoRefreshMinutes = 5;

    public ObservableCollection<AccountChoice> Accounts { get; } = [];

    public ObservableCollection<EmailMessageDto> Messages { get; } = [];

    public ObservableCollection<AttachmentRow> AttachmentRows { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(FetchCommand))]
    public partial AccountChoice? SelectedAccount { get; set; }

    [ObservableProperty]
    public partial EmailMessageDto? SelectedMessage { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ReplyCommand))]
    [NotifyCanExecuteChangedFor(nameof(ReplyAllCommand))]
    [NotifyCanExecuteChangedFor(nameof(ForwardCommand))]
    [NotifyCanExecuteChangedFor(nameof(RequestDeleteCommand))]
    [NotifyCanExecuteChangedFor(nameof(ConfirmDeleteCommand))]
    public partial EmailMessageDto? OpenMessage { get; set; }

    /// <summary>The open message's HTML reduced to blocks; empty when it has none.</summary>
    [ObservableProperty]
    public partial IReadOnlyList<HtmlBlock> FormattedBlocks { get; set; } = [];

    /// <summary>True when the open message has HTML to show formatted.</summary>
    [ObservableProperty]
    public partial bool HasHtml { get; set; }

    /// <summary>Formatted HTML (the default) or the plain text part; only matters when <see cref="HasHtml"/>.</summary>
    [ObservableProperty]
    public partial bool ShowFormatted { get; set; } = true;

    public bool ShowFormattedBody => HasHtml && ShowFormatted;

    public bool ShowPlainBody => !ShowFormattedBody;

    /// <summary>True when the message has HTML but the plain text is showing, so the way back is offered.</summary>
    public bool ShowPlainToggle => HasHtml && !ShowFormatted;

    [RelayCommand]
    private void ToggleFormatted() => ShowFormatted = !ShowFormatted;

    partial void OnHasHtmlChanged(bool value)
    {
        OnPropertyChanged(nameof(ShowFormattedBody));
        OnPropertyChanged(nameof(ShowPlainBody));
        OnPropertyChanged(nameof(ShowPlainToggle));
    }

    partial void OnShowFormattedChanged(bool value)
    {
        OnPropertyChanged(nameof(ShowFormattedBody));
        OnPropertyChanged(nameof(ShowPlainBody));
        OnPropertyChanged(nameof(ShowPlainToggle));
    }

    /// <summary>The reply or forward being written, shown in place of the reading pane; null when none.</summary>
    [ObservableProperty]
    public partial ComposeViewModel? Composer { get; set; }

    [ObservableProperty]
    public partial bool AutoRefresh { get; set; } = true;

    [ObservableProperty]
    public partial bool IsConfirmingDelete { get; set; }

    [ObservableProperty]
    public partial bool HasNoAccounts { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(FetchCommand))]
    public partial bool IsFetching { get; set; }

    [ObservableProperty]
    public partial string? StatusMessage { get; set; }

    [ObservableProperty]
    public partial bool StatusIsError { get; set; }

    // Bumped whenever the open message changes, so a slow load for a message that is no longer wanted cannot overwrite the current one.
    private int _openVersion;

    // Set while a reload swaps the list: the same message is selected again, which must not close or reopen it.
    private bool _suppressOpen;

    private int _pages = 1;

    /// <summary>True when there are older messages than the ones listed.</summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LoadMoreCommand))]
    public partial bool HasMore { get; set; }

    partial void OnSelectedAccountChanged(AccountChoice? value)
    {
        _pages = 1;
        CloseMessage();
        _ = ReloadMessagesAsync(CancellationToken.None);
    }

    partial void OnSelectedMessageChanged(EmailMessageDto? value)
    {
        if (_suppressOpen)
        {
            return;
        }

        IsConfirmingDelete = false;
        _ = OpenAsync(value, CancellationToken.None);
    }

    [RelayCommand(CanExecute = nameof(HasMore))]
    private async Task LoadMoreAsync(CancellationToken cancellationToken)
    {
        _pages++;
        await ReloadMessagesAsync(cancellationToken);
    }

    [RelayCommand]
    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        var response = await listAccounts.HandleAsync(new ListEmailAccounts.Query(userId), cancellationToken);

        var previous = SelectedAccount?.Id;
        Accounts.Clear();
        foreach (var account in response.Items.Where(a => a.Protocol == MailProtocol.Pop3))
        {
            Accounts.Add(AccountChoice.From(account));
        }

        HasNoAccounts = Accounts.Count == 0;
        SelectedAccount = Accounts.FirstOrDefault(a => a.Id == previous) ?? Accounts.FirstOrDefault();
    }

    [RelayCommand(CanExecute = nameof(CanFetch))]
    private Task FetchAsync(CancellationToken cancellationToken) => FetchCoreAsync(quiet: false, cancellationToken);

    /// <summary>Called by the view's timer. Does nothing when switched off or busy; stays silent unless something new arrived or it failed.</summary>
    [RelayCommand]
    private async Task AutoRefreshAsync(CancellationToken cancellationToken)
    {
        if (AutoRefresh && CanFetch())
        {
            await FetchCoreAsync(quiet: true, cancellationToken);
        }
    }

    private bool CanFetch() => SelectedAccount is not null && !IsFetching;

    private async Task FetchCoreAsync(bool quiet, CancellationToken cancellationToken)
    {
        if (SelectedAccount is not { } account)
        {
            return;
        }

        IsFetching = true;
        if (!quiet)
        {
            StatusIsError = false;
            StatusMessage = localization.Get("Inbox_Fetching");
        }

        try
        {
            var result = await fetch.HandleAsync(new FetchEmail.Command(userId, account.Id), cancellationToken);
            if (!result.IsSuccess)
            {
                StatusIsError = true;
                StatusMessage = Describe(result.Error);
            }
            else if (!quiet || result.Value > 0)
            {
                StatusIsError = false;
                StatusMessage = localization.Format("Inbox_Fetched", result.Value);
                await ReloadMessagesAsync(cancellationToken);
            }
        }
        finally
        {
            IsFetching = false;
        }
    }

    private async Task ReloadMessagesAsync(CancellationToken cancellationToken)
    {
        var items = new List<EmailMessageDto>();
        var total = 0;
        if (SelectedAccount is { } account)
        {
            // Reloads keep as many pages as the user has asked for.
            for (var page = 1; page <= _pages; page++)
            {
                var result = await listMessages.HandleAsync(new ListEmailMessages.Query(userId, account.Id, page, ListEmailMessages.MaxPageSize), cancellationToken);
                if (!result.IsSuccess)
                {
                    StatusIsError = true;
                    StatusMessage = localization.Describe(result.Error);
                    return;
                }

                items.AddRange(result.Value.Items);
                total = result.Value.TotalCount;
                if (items.Count >= total)
                {
                    break;
                }
            }
        }

        // A refresh must not close or reset the message the user is reading, so the selection is put back without reopening it.
        var keep = SelectedMessage?.Id;
        _suppressOpen = true;
        try
        {
            Messages.Clear();
            foreach (var message in items)
            {
                Messages.Add(message);
            }

            SelectedMessage = Messages.FirstOrDefault(m => m.Id == keep);
        }
        finally
        {
            _suppressOpen = false;
        }

        if (SelectedMessage is null && OpenMessage is not null)
        {
            CloseMessage();
        }

        HasMore = total > Messages.Count;

        if (!IsFetching)
        {
            StatusIsError = false;
            StatusMessage = localization.Format("Inbox_Count", total);
        }
    }

    private void CloseMessage()
    {
        _openVersion++;
        IsConfirmingDelete = false;
        ClearOpenMessage();
    }

    private void ClearOpenMessage()
    {
        AttachmentRows.Clear();
        FormattedBlocks = [];
        HasHtml = false;
        ShowFormatted = true;
        OpenMessage = null;
    }

    private async Task OpenAsync(EmailMessageDto? summary, CancellationToken cancellationToken)
    {
        if (summary is null)
        {
            CloseMessage();
            return;
        }

        var version = ++_openVersion;
        ClearOpenMessage();

        var result = await getMessage.HandleAsync(new GetEmailMessage.Query(summary.Id, userId), cancellationToken);
        if (version != _openVersion || !result.IsSuccess)
        {
            return;
        }

        OpenMessage = result.Value;
        if (!string.IsNullOrWhiteSpace(result.Value.HtmlBody))
        {
            FormattedBlocks = HtmlFormatter.Format(result.Value.HtmlBody);
            HasHtml = FormattedBlocks.Count > 0;
        }

        foreach (var attachment in result.Value.Attachments)
        {
            AttachmentRows.Add(new AttachmentRow(
                $"{attachment.FileName} ({SizeText.Format(attachment.Size)})",
                new AsyncRelayCommand(ct => SaveAttachmentAsync(attachment, ct))));
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

        var result = await deleteMessage.HandleAsync(new DeleteEmailMessage.Command(message.Id, userId), cancellationToken);

        IsConfirmingDelete = false;
        SelectedMessage = null;
        await ReloadMessagesAsync(cancellationToken);
        if (!result.IsSuccess)
        {
            StatusIsError = true;
            StatusMessage = localization.Describe(result.Error);
        }
    }

    private bool HasOpenMessage() => OpenMessage is not null;

    [RelayCommand(CanExecute = nameof(HasOpenMessage))]
    private async Task ReplyAsync(CancellationToken cancellationToken)
    {
        if (OpenMessage is not { } message)
        {
            return;
        }

        var quoted = string.Join('\n', message.Body.ReplaceLineEndings("\n").Split('\n').Select(line => "> " + line));
        var body = $"\n\n{localization.Format("Reply_Header", FormatDate(message.SentAt), message.From)}\n{quoted}";
        await StartComposerAsync(MessageRules.ExtractAddress(message.From), string.Empty, MessageRules.WithPrefix("Re: ", message.Subject), body, [], cancellationToken);
    }

    [RelayCommand(CanExecute = nameof(HasOpenMessage))]
    private async Task ReplyAllAsync(CancellationToken cancellationToken)
    {
        if (OpenMessage is not { } message)
        {
            return;
        }

        var (to, cc) = ReplyAllRecipients(message, SelectedAccount?.EmailAddress);
        var quoted = string.Join('\n', message.Body.ReplaceLineEndings("\n").Split('\n').Select(line => "> " + line));
        var body = $"\n\n{localization.Format("Reply_Header", FormatDate(message.SentAt), message.From)}\n{quoted}";
        await StartComposerAsync(to, cc, MessageRules.WithPrefix("Re: ", message.Subject), body, [], cancellationToken);
    }

    /// <summary>
    /// To: the sender plus everyone else the mail was addressed to; Cc: the original Cc. Our own address and repeats are left out.
    /// Mail fetched before recipients were stored has neither, which makes this an ordinary reply.
    /// </summary>
    internal static (string To, string Cc) ReplyAllRecipients(EmailMessageDto message, string? ownAddress)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrWhiteSpace(ownAddress))
        {
            seen.Add(ownAddress.Trim());
        }

        var sender = MessageRules.ExtractAddress(message.From);
        var to = new List<string>();
        if (seen.Add(sender))
        {
            to.Add(sender);
        }

        to.AddRange(MessageRules.ParseRecipients(message.To).Where(seen.Add));
        var cc = MessageRules.ParseRecipients(message.Cc).Where(seen.Add).ToList();
        return (string.Join(", ", to), string.Join(", ", cc));
    }

    [RelayCommand(CanExecute = nameof(HasOpenMessage))]
    private async Task ForwardAsync(CancellationToken cancellationToken)
    {
        if (OpenMessage is not { } message)
        {
            return;
        }

        var header = localization.Format("Forward_Header", message.From, FormatDate(message.SentAt), message.Subject);
        var attachments = new List<AttachmentInput>();
        foreach (var attachment in message.Attachments)
        {
            var content = await getAttachment.HandleAsync(new GetEmailAttachment.Query(attachment.Id, userId), cancellationToken);
            if (content.IsSuccess)
            {
                attachments.Add(new AttachmentInput(content.Value.FileName, content.Value.Content));
            }
        }

        await StartComposerAsync(string.Empty, string.Empty, MessageRules.WithPrefix("Fwd: ", message.Subject), $"\n\n{header}\n\n{message.Body}", attachments, cancellationToken);
    }

    private async Task StartComposerAsync(string to, string cc, string subject, string body, IEnumerable<AttachmentInput> attachments, CancellationToken cancellationToken)
    {
        CloseComposer();

        var composer = composeFactory();
        composer.Sent += OnComposerSent;
        composer.Cancelled += OnComposerCancelled;
        Composer = composer;

        // Prefer sending from the address the mail came in on.
        await composer.PrepareAsync(SelectedAccount?.EmailAddress, to, cc, subject, body, attachments, cancellationToken);
    }

    private void CloseComposer()
    {
        if (Composer is { } composer)
        {
            composer.Sent -= OnComposerSent;
            composer.Cancelled -= OnComposerCancelled;
            Composer = null;
        }
    }

    private void OnComposerSent(object? sender, EventArgs e)
    {
        CloseComposer();
        StatusIsError = false;
        StatusMessage = localization.Get("Inbox_ReplySent");
    }

    private void OnComposerCancelled(object? sender, EventArgs e) => CloseComposer();

    private static string FormatDate(DateTimeOffset value) => value.LocalDateTime.ToString("g", CultureInfo.CurrentCulture);

    // The server's own words follow our text for connection problems; they are what tells a wrong password from a wrong port.
    private string Describe(Error error)
    {
        var text = localization.Describe(error);
        return MailGatewayException.IsGatewayCode(error.Code) && !string.IsNullOrWhiteSpace(error.Message)
            ? $"{text} ({error.Message})"
            : text;
    }
}

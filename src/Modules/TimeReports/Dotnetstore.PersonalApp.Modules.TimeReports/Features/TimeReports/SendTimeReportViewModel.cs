using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dotnetstore.PersonalApp.Shared.Clients;
using Dotnetstore.PersonalApp.Shared.Contacts;
using Dotnetstore.PersonalApp.Shared.Email;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;

namespace Dotnetstore.PersonalApp.Modules.TimeReports.Features.TimeReports;

/// <summary>Who the mail is for: the client itself or one of its contact persons. <paramref name="Id"/> owns the email addresses.</summary>
internal sealed record RecipientOption(Guid Id, string Name);

internal sealed record EmailAddressOption(string Address, string Label);

/// <summary>
/// Exports one client's reports for a year and mails the workbook through the system SMTP account. The recipient is the client or one
/// of its contact persons, and the address is one of the email addresses registered for that recipient (chosen automatically when there is only one).
/// </summary>
internal sealed partial class SendTimeReportViewModel(
    Guid userId,
    Guid clientId,
    string clientName,
    int year,
    ExportTimeReports.Handler export,
    ISystemMailSender mailSender,
    IContactPersonLookup? contactPersons,
    IReadOnlyList<IContactInfoSource> contactSources,
    ILocalizationService localization) : ViewModelBase
{
    private bool _initializing;

    public ObservableCollection<RecipientOption> Recipients { get; } = [];

    public ObservableCollection<EmailAddressOption> Addresses { get; } = [];

    public string Title => localization.Format("SendTimeReport_Title", clientName, year);

    public event EventHandler<string>? Sent;

    public event EventHandler? Closed;

    [ObservableProperty]
    public partial RecipientOption? SelectedRecipient { get; set; }

    [ObservableProperty]
    public partial EmailAddressOption? SelectedAddress { get; set; }

    [ObservableProperty]
    public partial string Subject { get; set; } = localization.Format("SendTimeReport_DefaultSubject", clientName, year);

    [ObservableProperty]
    public partial string Body { get; set; } = localization.Format("SendTimeReport_DefaultBody", clientName, year);

    [ObservableProperty]
    public partial bool HasNoAddresses { get; set; }

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SendCommand))]
    public partial bool IsSending { get; set; }

    partial void OnSelectedRecipientChanged(RecipientOption? value)
    {
        if (!_initializing)
        {
            LoadAddressesCommand.Execute(null);
        }
    }

    /// <summary>Fills the recipient list (the client first, then its contact persons) and the addresses of the client.</summary>
    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        var client = new RecipientOption(clientId, clientName);
        Recipients.Add(client);
        if (contactPersons is not null)
        {
            foreach (var person in await contactPersons.ListAsync(clientId, cancellationToken))
            {
                Recipients.Add(new RecipientOption(person.Id, person.Name));
            }
        }

        _initializing = true;
        SelectedRecipient = client;
        _initializing = false;

        await LoadAddressesAsync(cancellationToken);
    }

    [RelayCommand]
    private async Task LoadAddressesAsync(CancellationToken cancellationToken)
    {
        Addresses.Clear();
        SelectedAddress = null;
        HasNoAddresses = false;

        if (SelectedRecipient is not { } recipient)
        {
            return;
        }

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var source in contactSources)
        {
            foreach (var entry in await source.ListAsync(recipient.Id, cancellationToken))
            {
                if (entry.Kind == ContactKind.Email && seen.Add(entry.Value))
                {
                    Addresses.Add(new EmailAddressOption(entry.Value, $"{entry.TypeName}: {entry.Value}"));
                }
            }
        }

        HasNoAddresses = Addresses.Count == 0;

        // With a single address there is nothing to choose.
        if (Addresses.Count == 1)
        {
            SelectedAddress = Addresses[0];
        }
    }

    [RelayCommand(CanExecute = nameof(CanSend))]
    private async Task SendAsync(CancellationToken cancellationToken)
    {
        ErrorMessage = null;

        if (SelectedAddress is not { } address)
        {
            ErrorMessage = localization.Get("SendTimeReport_SelectAddress");
            return;
        }

        IsSending = true;
        try
        {
            var exported = await export.HandleAsync(new ExportTimeReports.Query(userId, clientId, year), cancellationToken);
            if (!exported.IsSuccess)
            {
                ErrorMessage = localization.Describe(exported.Error);
                return;
            }

            var mail = new SystemMail(address.Address, Subject, Body, [new MailFile(exported.Value.FileName, exported.Value.Content)]);
            var sent = await mailSender.SendAsync(mail, cancellationToken);
            if (!sent.IsSuccess)
            {
                ErrorMessage = localization.Describe(sent.Error);
                return;
            }

            Sent?.Invoke(this, address.Address);
        }
        finally
        {
            IsSending = false;
        }
    }

    private bool CanSend() => !IsSending;

    [RelayCommand]
    private void Cancel() => Closed?.Invoke(this, EventArgs.Empty);
}

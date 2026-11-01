using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dotnetstore.PersonalApp.Modules.Email.Domain;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Modules.Email.Mail;
using Dotnetstore.PersonalApp.Shared.Mvvm;
using Dotnetstore.PersonalApp.Shared.Results;

namespace Dotnetstore.PersonalApp.Modules.Email.Features.EmailAccounts;

internal sealed partial class EmailAccountEditorViewModel : ViewModelBase
{
    private readonly EmailAccountDto? _existing;
    private readonly Guid _userId;
    private readonly CreateEmailAccount.Handler _create;
    private readonly UpdateEmailAccount.Handler _update;
    private readonly TestEmailConnection.Handler _test;
    private readonly ILocalizationService _localization;

    // The default port of the current protocol and security; the port follows them until the user types a different one.
    private int _suggestedPort;
    private bool _initialising = true;

    public EmailAccountEditorViewModel(
        EmailAccountDto? existing,
        Guid userId,
        CreateEmailAccount.Handler create,
        UpdateEmailAccount.Handler update,
        TestEmailConnection.Handler test,
        ILocalizationService localization)
    {
        _existing = existing;
        _userId = userId;
        _create = create;
        _update = update;
        _test = test;
        _localization = localization;

        Protocols =
        [
            new ProtocolChoice(MailProtocol.Smtp, localization.Get("Email_ProtocolSmtp")),
            new ProtocolChoice(MailProtocol.Pop3, localization.Get("Email_ProtocolPop3")),
        ];
        Securities =
        [
            new SecurityChoice(ConnectionSecurity.None, localization.Get("Email_SecurityNone")),
            new SecurityChoice(ConnectionSecurity.StartTls, localization.Get("Email_SecurityStartTls")),
            new SecurityChoice(ConnectionSecurity.SslOnConnect, localization.Get("Email_SecuritySsl")),
        ];

        var protocol = existing?.Protocol ?? MailProtocol.Smtp;
        var security = existing?.Security ?? ConnectionSecurity.StartTls;
        SelectedProtocol = Protocols.First(p => p.Value == protocol);
        SelectedSecurity = Securities.First(s => s.Value == security);
        _suggestedPort = EmailAccountRules.DefaultPort(protocol, security);
        PortText = (existing?.Port ?? _suggestedPort).ToString(CultureInfo.InvariantCulture);

        if (existing is not null)
        {
            Name = existing.Name;
            Host = existing.Host;
            EmailAddress = existing.EmailAddress;
            Username = existing.Username ?? string.Empty;
        }

        _initialising = false;
    }

    public event EventHandler<EmailAccountDto>? Saved;

    public event EventHandler? Cancelled;

    public IReadOnlyList<ProtocolChoice> Protocols { get; }

    public IReadOnlyList<SecurityChoice> Securities { get; }

    public string EditorTitle => _localization.Get(_existing is null ? "EmailEditor_New" : "EmailEditor_Edit");

    /// <summary>Personal or system is decided when the account is created and cannot be changed afterwards.</summary>
    public bool CanChooseScope => _existing is null;

    public string PasswordPlaceholder => _localization.Get(_existing?.HasPassword == true ? "EmailField_PasswordKeep" : "EmailField_PasswordPlaceholder");

    [ObservableProperty]
    public partial string Name { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsSystemAccount { get; set; }

    [ObservableProperty]
    public partial ProtocolChoice? SelectedProtocol { get; set; }

    [ObservableProperty]
    public partial SecurityChoice? SelectedSecurity { get; set; }

    [ObservableProperty]
    public partial string Host { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string PortText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string EmailAddress { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Username { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Password { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    [ObservableProperty]
    public partial string? TestMessage { get; set; }

    [ObservableProperty]
    public partial bool TestSucceeded { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(TestConnectionCommand))]
    public partial bool IsTesting { get; set; }

    partial void OnSelectedProtocolChanged(ProtocolChoice? value) => FollowSuggestedPort();

    partial void OnSelectedSecurityChanged(SecurityChoice? value) => FollowSuggestedPort();

    private void FollowSuggestedPort()
    {
        if (_initialising || SelectedProtocol is null || SelectedSecurity is null)
        {
            return;
        }

        var next = EmailAccountRules.DefaultPort(SelectedProtocol.Value, SelectedSecurity.Value);
        if (PortText == _suggestedPort.ToString(CultureInfo.InvariantCulture))
        {
            PortText = next.ToString(CultureInfo.InvariantCulture);
        }

        _suggestedPort = next;
    }

    [RelayCommand]
    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        ErrorMessage = null;

        // Unparsable text becomes 0, which the port rule rejects with a translated message.
        var port = int.TryParse(PortText, NumberStyles.None, CultureInfo.InvariantCulture, out var parsed) ? parsed : 0;
        var protocol = SelectedProtocol?.Value ?? MailProtocol.Smtp;
        var security = SelectedSecurity?.Value ?? ConnectionSecurity.StartTls;
        var password = string.IsNullOrEmpty(Password) ? null : Password;

        var result = _existing is null
            ? await _create.HandleAsync(
                new CreateEmailAccount.Command(_userId, IsSystemAccount, Name, protocol, Host, port, security, EmailAddress, Username, password),
                cancellationToken)
            : await _update.HandleAsync(
                new UpdateEmailAccount.Command(_existing.Id, _userId, Name, protocol, Host, port, security, EmailAddress, Username, password),
                cancellationToken);

        if (result.IsSuccess)
        {
            Saved?.Invoke(this, result.Value);
        }
        else
        {
            ErrorMessage = _localization.Describe(result.Error);
        }
    }

    [RelayCommand(CanExecute = nameof(CanTest))]
    private async Task TestConnectionAsync(CancellationToken cancellationToken)
    {
        TestMessage = null;
        TestSucceeded = false;
        IsTesting = true;
        try
        {
            var port = int.TryParse(PortText, NumberStyles.None, CultureInfo.InvariantCulture, out var parsed) ? parsed : 0;
            var result = await _test.HandleAsync(
                new TestEmailConnection.Command(
                    _userId,
                    _existing?.Id,
                    SelectedProtocol?.Value ?? MailProtocol.Smtp,
                    Host,
                    port,
                    SelectedSecurity?.Value ?? ConnectionSecurity.StartTls,
                    Username,
                    string.IsNullOrEmpty(Password) ? null : Password),
                cancellationToken);

            TestSucceeded = result.IsSuccess;
            TestMessage = result.IsSuccess ? _localization.Get("EmailEditor_TestOk") : DescribeFailure(result.Error);
        }
        finally
        {
            IsTesting = false;
        }
    }

    private bool CanTest() => !IsTesting;

    // The server's own words ("535 Authentication failed") help more than our generic text, so they follow it.
    private string DescribeFailure(Error error)
    {
        var text = _localization.Describe(error);
        return MailGatewayException.IsGatewayCode(error.Code) && !string.IsNullOrWhiteSpace(error.Message)
            ? $"{text} ({error.Message})"
            : text;
    }

    [RelayCommand]
    private void Cancel() => Cancelled?.Invoke(this, EventArgs.Empty);
}

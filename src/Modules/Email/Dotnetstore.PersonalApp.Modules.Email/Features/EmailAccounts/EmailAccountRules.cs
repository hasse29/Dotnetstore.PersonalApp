using System.Net.Mail;
using Dotnetstore.PersonalApp.Modules.Email.Domain;
using Dotnetstore.PersonalApp.Shared.Results;
using FluentValidation;
using FluentValidation.Results;

namespace Dotnetstore.PersonalApp.Modules.Email.Features.EmailAccounts;

/// <summary>
/// Shared validation rules. Each rule carries an error code (e.g. <c>email_account_name_required</c>) that the UI translates via the
/// <c>Error_{code}</c> resource; the English message is only a fallback.
/// </summary>
internal static class EmailAccountRules
{
    public const int MaxNameLength = 100;
    public const int MaxHostLength = 255;
    public const int MaxEmailAddressLength = 254;
    public const int MaxUsernameLength = 200;
    public const int MaxPasswordLength = 500;

    public static IRuleBuilderOptions<T, Guid> UserIdRules<T>(this IRuleBuilder<T, Guid> rule) =>
        rule.NotEmpty().WithErrorCode("email_account_user_required").WithMessage("Nobody is signed in.");

    public static IRuleBuilderOptions<T, string> NameRules<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty().WithErrorCode("email_account_name_required").WithMessage("Name is required.")
            .MaximumLength(MaxNameLength).WithErrorCode("email_account_name_too_long").WithMessage("Name is too long.");

    public static IRuleBuilderOptions<T, MailProtocol> ProtocolRules<T>(this IRuleBuilder<T, MailProtocol> rule) =>
        rule.IsInEnum().WithErrorCode("email_account_protocol_invalid").WithMessage("Protocol must be SMTP or POP3.");

    public static IRuleBuilderOptions<T, string> HostRules<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty().WithErrorCode("email_account_host_required").WithMessage("Server is required.")
            .MaximumLength(MaxHostLength).WithErrorCode("email_account_host_too_long").WithMessage("Server is too long.")
            .Must(host => Uri.CheckHostName(host.Trim()) != UriHostNameType.Unknown)
            .WithErrorCode("email_account_host_invalid").WithMessage("The server name is not valid.");

    public static IRuleBuilderOptions<T, int> PortRules<T>(this IRuleBuilder<T, int> rule) =>
        rule.InclusiveBetween(1, 65535).WithErrorCode("email_account_port_invalid").WithMessage("Port must be between 1 and 65535.");

    public static IRuleBuilderOptions<T, ConnectionSecurity> SecurityRules<T>(this IRuleBuilder<T, ConnectionSecurity> rule) =>
        rule.IsInEnum().WithErrorCode("email_account_security_invalid").WithMessage("The connection security is not valid.");

    public static IRuleBuilderOptions<T, string> EmailAddressRules<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty().WithErrorCode("email_account_address_required").WithMessage("Email address is required.")
            .MaximumLength(MaxEmailAddressLength).WithErrorCode("email_account_address_too_long").WithMessage("Email address is too long.")
            .Must(IsValidAddress).WithErrorCode("email_account_address_invalid").WithMessage("The email address is not valid.");

    public static IRuleBuilderOptions<T, string?> UsernameRules<T>(this IRuleBuilder<T, string?> rule) =>
        rule.MaximumLength(MaxUsernameLength).WithErrorCode("email_account_username_too_long").WithMessage("Username is too long.");

    public static IRuleBuilderOptions<T, string?> PasswordRules<T>(this IRuleBuilder<T, string?> rule) =>
        rule.MaximumLength(MaxPasswordLength).WithErrorCode("email_account_password_too_long").WithMessage("Password is too long.");

    /// <summary>The usual port for a protocol and security combination; used as the starting value in the editor.</summary>
    public static int DefaultPort(MailProtocol protocol, ConnectionSecurity security) => (protocol, security) switch
    {
        (MailProtocol.Smtp, ConnectionSecurity.None) => 25,
        (MailProtocol.Smtp, ConnectionSecurity.StartTls) => 587,
        (MailProtocol.Smtp, ConnectionSecurity.SslOnConnect) => 465,
        (MailProtocol.Pop3, ConnectionSecurity.SslOnConnect) => 995,
        _ => 110,
    };

    /// <summary>True when the account already points at this server, port and login, so its stored password is still meant for it.</summary>
    public static bool SameServer(EmailAccount account, string host, int port, string? username) =>
        string.Equals(account.Host, host.Trim(), StringComparison.OrdinalIgnoreCase)
        && account.Port == port
        && string.Equals(account.Username, username, StringComparison.Ordinal);

    /// <summary>Optional text fields: trimmed, and empty becomes null.</summary>
    public static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    /// <summary>True for a plain address such as <c>anna@example.com</c>; display names like <c>Anna &lt;anna@example.com&gt;</c> are rejected.</summary>
    public static bool IsValidAddress(string value)
    {
        var text = value.Trim();
        return MailAddress.TryCreate(text, out var address) && address.Address == text;
    }

    public static Error ToError(this ValidationResult result)
    {
        var failure = result.Errors[0];
        return Error.Validation(failure.ErrorCode, failure.ErrorMessage);
    }
}

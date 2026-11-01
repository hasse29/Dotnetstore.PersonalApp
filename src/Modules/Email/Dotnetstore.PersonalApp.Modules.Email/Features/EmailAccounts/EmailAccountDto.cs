using Dotnetstore.PersonalApp.Modules.Email.Domain;

namespace Dotnetstore.PersonalApp.Modules.Email.Features.EmailAccounts;

/// <summary>An account as shown in the UI. The password never leaves the handlers; <see cref="HasPassword"/> only says whether one is stored.</summary>
internal sealed record EmailAccountDto(
    Guid Id,
    bool IsSystem,
    string Name,
    MailProtocol Protocol,
    string Host,
    int Port,
    ConnectionSecurity Security,
    string EmailAddress,
    string? Username,
    bool HasPassword,
    DateTimeOffset UpdatedAt)
{
    public static EmailAccountDto From(EmailAccount account) => new(
        account.Id,
        account.IsSystem,
        account.Name,
        account.Protocol,
        account.Host,
        account.Port,
        account.Security,
        account.EmailAddress,
        account.Username,
        !string.IsNullOrEmpty(account.ProtectedPassword),
        account.UpdatedAt);
}

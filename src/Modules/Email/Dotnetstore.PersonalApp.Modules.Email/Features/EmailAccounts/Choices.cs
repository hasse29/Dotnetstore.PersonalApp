using Dotnetstore.PersonalApp.Modules.Email.Domain;

namespace Dotnetstore.PersonalApp.Modules.Email.Features.EmailAccounts;

/// <summary>A protocol in the editor's drop-down, with its translated label.</summary>
internal sealed record ProtocolChoice(MailProtocol Value, string Label);

/// <summary>A connection security option in the editor's drop-down, with its translated label.</summary>
internal sealed record SecurityChoice(ConnectionSecurity Value, string Label);

/// <summary>One line of the account table, with the texts already translated.</summary>
internal sealed record EmailAccountRow(EmailAccountDto Account, string ProtocolText, string Server, string ScopeText)
{
    public string Name => Account.Name;
}

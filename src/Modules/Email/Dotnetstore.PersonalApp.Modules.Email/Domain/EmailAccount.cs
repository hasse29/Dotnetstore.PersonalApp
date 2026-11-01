namespace Dotnetstore.PersonalApp.Modules.Email.Domain;

/// <summary>
/// One SMTP or POP3 account. A personal account belongs to a user (<see cref="OwnerId"/> refers to an object owned by the Users
/// module; it is not a foreign key, and the user's accounts are removed through the <c>UserDeleted</c> integration event).
/// A system account is shared by everyone and has <see cref="SystemOwner"/> as owner.
/// </summary>
internal sealed class EmailAccount
{
    /// <summary>The owner of system accounts. An empty id rather than null so the unique (owner, name) index also covers them.</summary>
    public static readonly Guid SystemOwner = Guid.Empty;

    public Guid Id { get; set; }

    public Guid OwnerId { get; set; }

    public bool IsSystem => OwnerId == SystemOwner;

    /// <summary>Unique per owner, ignoring case, so the accounts can be told apart.</summary>
    public string Name { get; set; } = string.Empty;

    public MailProtocol Protocol { get; set; }

    public string Host { get; set; } = string.Empty;

    public int Port { get; set; }

    public ConnectionSecurity Security { get; set; }

    /// <summary>The mailbox address: the sender for SMTP, the mailbox that is read for POP3.</summary>
    public string EmailAddress { get; set; } = string.Empty;

    /// <summary>The login, or null for a server that needs none.</summary>
    public string? Username { get; set; }

    /// <summary>The password as protected by <c>ISecretProtector</c>; never the plain text.</summary>
    public string? ProtectedPassword { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}

namespace Dotnetstore.PersonalApp.Modules.Email.Domain;

/// <summary>
/// A message the user started writing and saved for later. Drafts are personal: <see cref="UserId"/> refers to an object owned by
/// the Users module (not a foreign key; a deleted user's drafts go through the <c>UserDeleted</c> integration event).
/// <see cref="AccountId"/> is the account the user had chosen; if that account is gone the composer simply offers another.
/// </summary>
internal sealed class Draft
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public Guid? AccountId { get; set; }

    public string To { get; set; } = string.Empty;

    public string Cc { get; set; } = string.Empty;

    public string Subject { get; set; } = string.Empty;

    public string Body { get; set; } = string.Empty;

    public bool IsHtml { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}

namespace Dotnetstore.PersonalApp.Modules.EmailAddresses.Domain;

/// <summary>
/// An email address belonging to some object in another module (a user, client, customer, ...). Ids are globally unique GUIDs,
/// so <see cref="ObjectId"/> alone identifies the owner; this module never looks the owner up.
/// </summary>
internal sealed class EmailAddress
{
    public Guid Id { get; set; }

    public Guid ObjectId { get; set; }

    public Guid EmailAddressTypeId { get; set; }

    public EmailAddressType EmailAddressType { get; set; } = null!;

    public string Email { get; set; } = string.Empty;
}
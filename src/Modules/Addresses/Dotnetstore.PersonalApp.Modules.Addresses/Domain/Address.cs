namespace Dotnetstore.PersonalApp.Modules.Addresses.Domain;

/// <summary>
/// An address belonging to some object in another module (a user, client, customer, ...). Ids are globally unique GUIDs,
/// so <see cref="ObjectId"/> alone identifies the owner; this module never looks the owner up.
/// </summary>
internal sealed class Address
{
    public Guid Id { get; set; }

    public Guid ObjectId { get; set; }

    public Guid AddressTypeId { get; set; }

    public AddressType AddressType { get; set; } = null!;

    public string? CareOf { get; set; }

    public string Street1 { get; set; } = string.Empty;

    public string? Street2 { get; set; }

    public string PostalCode { get; set; } = string.Empty;

    public string City { get; set; } = string.Empty;

    public string? State { get; set; }

    public string Country { get; set; } = string.Empty;
}

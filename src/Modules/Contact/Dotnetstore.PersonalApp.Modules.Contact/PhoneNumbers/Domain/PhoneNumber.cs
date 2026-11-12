namespace Dotnetstore.PersonalApp.Modules.Contact.PhoneNumbers.Domain;

/// <summary>
/// A phone number belonging to some object in another module (a user, client, customer, ...). Ids are globally unique GUIDs,
/// so <see cref="ObjectId"/> alone identifies the owner; this module never looks the owner up.
/// </summary>
internal sealed class PhoneNumber
{
    public Guid Id { get; set; }

    public Guid ObjectId { get; set; }

    public Guid PhoneNumberTypeId { get; set; }

    public PhoneNumberType PhoneNumberType { get; set; } = null!;

    /// <summary>The number as the user typed it, e.g. <c>+46 8 123 456</c>.</summary>
    public string Number { get; set; } = string.Empty;

    /// <summary>Optional leading <c>+</c> followed by digits only; used to spot duplicates however the number is formatted.</summary>
    public string NormalizedNumber { get; set; } = string.Empty;
}
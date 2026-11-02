using Dotnetstore.PersonalApp.Modules.Clients.Domain;

namespace Dotnetstore.PersonalApp.Modules.Clients.Features.ContactPersons;

internal sealed record ContactPersonDto(
    Guid Id,
    Guid ClientId,
    string FirstName,
    string LastName,
    string? JobTitle,
    string? Notes,
    DateTimeOffset CreatedAt,
    byte[]? Thumbnail = null)
{
    public string FullName => $"{FirstName} {LastName}";

    public string Initials => string.Concat(char.ToUpperInvariant(FirstName[0]), char.ToUpperInvariant(LastName[0]));

    public static ContactPersonDto From(ContactPerson person) => new(
        person.Id,
        person.ClientId,
        person.FirstName,
        person.LastName,
        person.JobTitle,
        person.Notes,
        person.CreatedAt,
        person.PhotoThumbnail);
}

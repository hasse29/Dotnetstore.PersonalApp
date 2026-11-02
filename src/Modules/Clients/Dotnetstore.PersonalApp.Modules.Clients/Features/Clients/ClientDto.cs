using Dotnetstore.PersonalApp.Modules.Clients.Domain;
using Dotnetstore.PersonalApp.Shared.Photos;

namespace Dotnetstore.PersonalApp.Modules.Clients.Features.Clients;

internal sealed record ClientDto(
    Guid Id,
    string Name,
    string OrganizationNumber,
    string? VatNumber,
    string? Email,
    string? Phone,
    string? Website,
    string? Notes,
    DateTimeOffset CreatedAt,
    byte[]? Thumbnail = null)
{
    public string Initials => PhotoInitials.From(Name);

    public static ClientDto From(Client client) => new(
        client.Id,
        client.Name,
        client.OrganizationNumber,
        client.VatNumber,
        client.Email,
        client.Phone,
        client.Website,
        client.Notes,
        client.CreatedAt,
        client.PhotoThumbnail);
}

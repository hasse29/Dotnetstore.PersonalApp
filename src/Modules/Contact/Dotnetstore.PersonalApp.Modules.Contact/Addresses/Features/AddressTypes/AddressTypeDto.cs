using Dotnetstore.PersonalApp.Modules.Contact.Addresses.Domain;

namespace Dotnetstore.PersonalApp.Modules.Contact.Addresses.Features.AddressTypes;

internal sealed record AddressTypeDto(Guid Id, string Name, string? Description, DateTimeOffset CreatedAt)
{
    public static AddressTypeDto From(AddressType type) => new(type.Id, type.Name, type.Description, type.CreatedAt);
}

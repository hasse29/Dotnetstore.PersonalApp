using Dotnetstore.PersonalApp.Modules.Addresses.Domain;

namespace Dotnetstore.PersonalApp.Modules.Addresses.Features.AddressTypes;

internal sealed record AddressTypeDto(Guid Id, string Name, string? Description, DateTimeOffset CreatedAt)
{
    public static AddressTypeDto From(AddressType type) => new(type.Id, type.Name, type.Description, type.CreatedAt);
}

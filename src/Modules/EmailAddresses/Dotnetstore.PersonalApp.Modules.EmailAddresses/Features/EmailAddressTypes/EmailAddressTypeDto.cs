using Dotnetstore.PersonalApp.Modules.EmailAddresses.Domain;

namespace Dotnetstore.PersonalApp.Modules.EmailAddresses.Features.EmailAddressTypes;

internal sealed record EmailAddressTypeDto(Guid Id, string Name, string? Description, DateTimeOffset CreatedAt)
{
    public static EmailAddressTypeDto From(EmailAddressType type) => new(type.Id, type.Name, type.Description, type.CreatedAt);
}

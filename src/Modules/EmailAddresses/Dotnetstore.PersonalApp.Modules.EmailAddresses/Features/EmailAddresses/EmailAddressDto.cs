using Dotnetstore.PersonalApp.Modules.EmailAddresses.Domain;

namespace Dotnetstore.PersonalApp.Modules.EmailAddresses.Features.EmailAddresses;

internal sealed record EmailAddressDto(Guid Id, Guid ObjectId, Guid EmailAddressTypeId, string EmailAddressTypeName, string Email)
{
    public static EmailAddressDto From(EmailAddress address, string typeName) =>
        new(address.Id, address.ObjectId, address.EmailAddressTypeId, typeName, address.Email);
}
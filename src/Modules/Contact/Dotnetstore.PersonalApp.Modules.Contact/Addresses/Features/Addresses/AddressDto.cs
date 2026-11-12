using Dotnetstore.PersonalApp.Modules.Contact.Addresses.Domain;

namespace Dotnetstore.PersonalApp.Modules.Contact.Addresses.Features.Addresses;

internal sealed record AddressDto(
    Guid Id,
    Guid ObjectId,
    Guid AddressTypeId,
    string AddressTypeName,
    string? CareOf,
    string Street1,
    string? Street2,
    string PostalCode,
    string City,
    string? State,
    string Country)
{
    public string Summary => $"{Street1}, {PostalCode} {City}";

    public static AddressDto From(Address address, string addressTypeName) => new(
        address.Id,
        address.ObjectId,
        address.AddressTypeId,
        addressTypeName,
        address.CareOf,
        address.Street1,
        address.Street2,
        address.PostalCode,
        address.City,
        address.State,
        address.Country);
}

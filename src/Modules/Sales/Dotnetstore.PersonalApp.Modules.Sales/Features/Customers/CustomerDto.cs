using Dotnetstore.PersonalApp.Modules.Sales.Domain;

namespace Dotnetstore.PersonalApp.Modules.Sales.Features.Customers;

internal sealed record CustomerDto(
    Guid Id,
    string Name,
    string? OrganizationNumber,
    string? Notes,
    bool IsActive,
    byte[]? Thumbnail = null)
{
    public string Initials => InitialsOf(Name);

    public static string InitialsOf(string name) => string.Concat(
        name.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Take(2)
            .Select(part => char.ToUpperInvariant(part[0])));

    public static CustomerDto From(Customer customer) => new(
        customer.Id,
        customer.Name,
        customer.OrganizationNumber,
        customer.Notes,
        customer.IsActive,
        customer.PhotoThumbnail);
}

/// <summary>A customer as the list shows it: the DTO plus its status in the current UI language.</summary>
internal sealed record CustomerRow(CustomerDto Customer, string StatusText);

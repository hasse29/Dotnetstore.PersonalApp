using Dotnetstore.PersonalApp.Modules.Sales.Domain;

namespace Dotnetstore.PersonalApp.Modules.Sales.Features.Customers;

internal sealed record CustomerDto(
    Guid Id,
    string Name,
    string? OrganizationNumber,
    string? Email,
    string? Phone,
    string? Street,
    string? PostalCode,
    string? City,
    string? Notes,
    bool IsActive)
{
    public static CustomerDto From(Customer customer) => new(
        customer.Id,
        customer.Name,
        customer.OrganizationNumber,
        customer.Email,
        customer.Phone,
        customer.Street,
        customer.PostalCode,
        customer.City,
        customer.Notes,
        customer.IsActive);
}

/// <summary>A customer as the list shows it: the DTO plus its status in the current UI language.</summary>
internal sealed record CustomerRow(CustomerDto Customer, string StatusText);

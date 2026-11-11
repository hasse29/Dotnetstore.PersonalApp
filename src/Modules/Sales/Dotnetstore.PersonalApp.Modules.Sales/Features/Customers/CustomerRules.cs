using Dotnetstore.PersonalApp.Modules.Sales.Domain;
using Dotnetstore.PersonalApp.Shared.Results;
using FluentValidation;
using FluentValidation.Results;

namespace Dotnetstore.PersonalApp.Modules.Sales.Features.Customers;

/// <summary>What the create and update commands share.</summary>
internal sealed record CustomerFields(
    string Name,
    string? OrganizationNumber,
    string? Email,
    string? Phone,
    string? Street,
    string? PostalCode,
    string? City,
    string? Notes,
    bool IsActive);

/// <summary>
/// Shared validation rules. Each rule carries an error code (<c>customer_*</c>) that the UI translates via the
/// <c>Error_{code}</c> resource; the English message is only a fallback.
/// </summary>
internal static class CustomerRules
{
    public const int MaxNameLength = 200;
    public const int MaxOrganizationNumberLength = 30;
    public const int MaxEmailLength = 200;
    public const int MaxPhoneLength = 50;
    public const int MaxStreetLength = 200;
    public const int MaxPostalCodeLength = 20;
    public const int MaxCityLength = 100;
    public const int MaxNotesLength = 2000;

    public static IRuleBuilderOptions<T, string> NameRules<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty().WithErrorCode("customer_name_required").WithMessage("Name is required.")
            .MaximumLength(MaxNameLength).WithErrorCode("customer_name_too_long").WithMessage("Name is too long.");

    public static IRuleBuilderOptions<T, string?> OrganizationNumberRules<T>(this IRuleBuilder<T, string?> rule) =>
        rule.MaximumLength(MaxOrganizationNumberLength).WithErrorCode("customer_organization_number_too_long")
            .WithMessage("Organization number is too long.");

    public static IRuleBuilderOptions<T, string?> EmailRules<T>(this IRuleBuilder<T, string?> rule) =>
        rule.MaximumLength(MaxEmailLength).WithErrorCode("customer_email_too_long").WithMessage("Email is too long.")
            .Must(e => string.IsNullOrWhiteSpace(e) || IsEmail(e.Trim())).WithErrorCode("customer_email_invalid").WithMessage("Email is not valid.");

    public static IRuleBuilderOptions<T, string?> PhoneRules<T>(this IRuleBuilder<T, string?> rule) =>
        rule.MaximumLength(MaxPhoneLength).WithErrorCode("customer_phone_too_long").WithMessage("Phone is too long.");

    public static IRuleBuilderOptions<T, string?> StreetRules<T>(this IRuleBuilder<T, string?> rule) =>
        rule.MaximumLength(MaxStreetLength).WithErrorCode("customer_street_too_long").WithMessage("Street is too long.");

    public static IRuleBuilderOptions<T, string?> PostalCodeRules<T>(this IRuleBuilder<T, string?> rule) =>
        rule.MaximumLength(MaxPostalCodeLength).WithErrorCode("customer_postal_code_too_long").WithMessage("Postal code is too long.");

    public static IRuleBuilderOptions<T, string?> CityRules<T>(this IRuleBuilder<T, string?> rule) =>
        rule.MaximumLength(MaxCityLength).WithErrorCode("customer_city_too_long").WithMessage("City is too long.");

    public static IRuleBuilderOptions<T, string?> NotesRules<T>(this IRuleBuilder<T, string?> rule) =>
        rule.MaximumLength(MaxNotesLength).WithErrorCode("customer_notes_too_long").WithMessage("Notes are too long.");

    /// <summary>Optional text fields: trimmed, and empty becomes null.</summary>
    public static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    public static void Apply(Customer customer, CustomerFields fields)
    {
        customer.Name = fields.Name.Trim();
        customer.OrganizationNumber = NormalizeOptional(fields.OrganizationNumber);
        customer.Email = NormalizeOptional(fields.Email);
        customer.Phone = NormalizeOptional(fields.Phone);
        customer.Street = NormalizeOptional(fields.Street);
        customer.PostalCode = NormalizeOptional(fields.PostalCode);
        customer.City = NormalizeOptional(fields.City);
        customer.Notes = NormalizeOptional(fields.Notes);
        customer.IsActive = fields.IsActive;
    }

    /// <summary>The address as it is copied onto an offer: street, then postal code and city; null when there is none.</summary>
    public static string? FormatAddress(Customer customer)
    {
        var place = string.Join(' ', new[] { customer.PostalCode, customer.City }.Where(p => !string.IsNullOrWhiteSpace(p)));
        var lines = new[] { customer.Street, place }.Where(p => !string.IsNullOrWhiteSpace(p));
        var address = string.Join('\n', lines);
        return address.Length == 0 ? null : address;
    }

    public static Error ToError(this ValidationResult result)
    {
        var failure = result.Errors[0];
        return Error.Validation(failure.ErrorCode, failure.ErrorMessage);
    }

    // Deliberately loose: one "@" with text on both sides and a dot in the domain. The mail server decides the rest.
    private static bool IsEmail(string value)
    {
        var at = value.IndexOf('@', StringComparison.Ordinal);
        return at > 0
            && at == value.LastIndexOf('@')
            && value.IndexOf('.', at) > at + 1
            && !value.EndsWith('.')
            && !value.Contains(' ', StringComparison.Ordinal);
    }
}

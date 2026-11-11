using Dotnetstore.PersonalApp.Modules.Products.Domain;
using Dotnetstore.PersonalApp.Shared.Results;
using FluentValidation;
using FluentValidation.Results;

namespace Dotnetstore.PersonalApp.Modules.Products.Features.Suppliers;

/// <summary>What the create and update commands share.</summary>
internal sealed record SupplierFields(
    string Name,
    string? OrganizationNumber,
    string? Email,
    string? Phone,
    string? Website,
    string? Notes,
    bool IsActive);

/// <summary>
/// Shared validation rules. Each rule carries an error code (<c>supplier_*</c>) that the UI translates via the
/// <c>Error_{code}</c> resource; the English message is only a fallback.
/// </summary>
internal static class SupplierRules
{
    public const int MaxNameLength = 200;
    public const int MaxOrganizationNumberLength = 30;
    public const int MaxEmailLength = 200;
    public const int MaxPhoneLength = 50;
    public const int MaxWebsiteLength = 200;
    public const int MaxNotesLength = 2000;

    public static IRuleBuilderOptions<T, string> NameRules<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty().WithErrorCode("supplier_name_required").WithMessage("Name is required.")
            .MaximumLength(MaxNameLength).WithErrorCode("supplier_name_too_long").WithMessage("Name is too long.");

    public static IRuleBuilderOptions<T, string?> OrganizationNumberRules<T>(this IRuleBuilder<T, string?> rule) =>
        rule.MaximumLength(MaxOrganizationNumberLength).WithErrorCode("supplier_organization_number_too_long")
            .WithMessage("Organization number is too long.");

    public static IRuleBuilderOptions<T, string?> EmailRules<T>(this IRuleBuilder<T, string?> rule) =>
        rule.MaximumLength(MaxEmailLength).WithErrorCode("supplier_email_too_long").WithMessage("Email is too long.")
            .Must(e => string.IsNullOrWhiteSpace(e) || IsEmail(e.Trim())).WithErrorCode("supplier_email_invalid").WithMessage("Email is not valid.");

    public static IRuleBuilderOptions<T, string?> PhoneRules<T>(this IRuleBuilder<T, string?> rule) =>
        rule.MaximumLength(MaxPhoneLength).WithErrorCode("supplier_phone_too_long").WithMessage("Phone is too long.");

    public static IRuleBuilderOptions<T, string?> WebsiteRules<T>(this IRuleBuilder<T, string?> rule) =>
        rule.MaximumLength(MaxWebsiteLength).WithErrorCode("supplier_website_too_long").WithMessage("Website is too long.");

    public static IRuleBuilderOptions<T, string?> NotesRules<T>(this IRuleBuilder<T, string?> rule) =>
        rule.MaximumLength(MaxNotesLength).WithErrorCode("supplier_notes_too_long").WithMessage("Notes are too long.");

    /// <summary>Optional text fields: trimmed, and empty becomes null.</summary>
    public static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    public static void Apply(Supplier supplier, SupplierFields fields)
    {
        supplier.Name = fields.Name.Trim();
        supplier.OrganizationNumber = NormalizeOptional(fields.OrganizationNumber);
        supplier.Email = NormalizeOptional(fields.Email);
        supplier.Phone = NormalizeOptional(fields.Phone);
        supplier.Website = NormalizeOptional(fields.Website);
        supplier.Notes = NormalizeOptional(fields.Notes);
        supplier.IsActive = fields.IsActive;
    }

    public static Error ToError(this ValidationResult result)
    {
        var failure = result.Errors[0];
        return Error.Validation(failure.ErrorCode, failure.ErrorMessage);
    }

    // Deliberately loose: one "@" with text on both sides and a dot in the domain. The supplier's mail server decides the rest.
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

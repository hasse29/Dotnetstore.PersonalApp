using Dotnetstore.PersonalApp.Modules.Sales.Domain;
using Dotnetstore.PersonalApp.Shared.Results;
using FluentValidation;
using FluentValidation.Results;

namespace Dotnetstore.PersonalApp.Modules.Sales.Features.Customers;

/// <summary>What the create and update commands share.</summary>
internal sealed record CustomerFields(
    string Name,
    string? OrganizationNumber,
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
    public const int MaxNotesLength = 2000;

    public static IRuleBuilderOptions<T, string> NameRules<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty().WithErrorCode("customer_name_required").WithMessage("Name is required.")
            .MaximumLength(MaxNameLength).WithErrorCode("customer_name_too_long").WithMessage("Name is too long.");

    public static IRuleBuilderOptions<T, string?> OrganizationNumberRules<T>(this IRuleBuilder<T, string?> rule) =>
        rule.MaximumLength(MaxOrganizationNumberLength).WithErrorCode("customer_organization_number_too_long")
            .WithMessage("Organization number is too long.");

    public static IRuleBuilderOptions<T, string?> NotesRules<T>(this IRuleBuilder<T, string?> rule) =>
        rule.MaximumLength(MaxNotesLength).WithErrorCode("customer_notes_too_long").WithMessage("Notes are too long.");

    /// <summary>Optional text fields: trimmed, and empty becomes null.</summary>
    public static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    public static void Apply(Customer customer, CustomerFields fields)
    {
        customer.Name = fields.Name.Trim();
        customer.OrganizationNumber = NormalizeOptional(fields.OrganizationNumber);
        customer.Notes = NormalizeOptional(fields.Notes);
        customer.IsActive = fields.IsActive;
    }

    public static Error ToError(this ValidationResult result)
    {
        var failure = result.Errors[0];
        return Error.Validation(failure.ErrorCode, failure.ErrorMessage);
    }

}

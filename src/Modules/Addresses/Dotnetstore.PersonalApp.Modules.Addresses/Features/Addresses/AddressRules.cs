using Dotnetstore.PersonalApp.Shared.Results;
using FluentValidation;
using FluentValidation.Results;

namespace Dotnetstore.PersonalApp.Modules.Addresses.Features.Addresses;

/// <summary>
/// Shared validation rules. Each rule carries an error code (e.g. <c>address_city_required</c>) that the UI translates via
/// the <c>Error_{code}</c> resource; the English message is only a fallback.
/// </summary>
internal static class AddressRules
{
    public const int MaxCareOfLength = 100;
    public const int MaxStreetLength = 200;
    public const int MaxPostalCodeLength = 20;
    public const int MaxCityLength = 100;
    public const int MaxStateLength = 100;
    public const int MaxCountryLength = 100;

    public static IRuleBuilderOptions<T, Guid> ObjectIdRules<T>(this IRuleBuilder<T, Guid> rule) =>
        rule.NotEmpty().WithErrorCode("address_object_required").WithMessage("The address must belong to an object.");

    public static IRuleBuilderOptions<T, Guid> AddressTypeIdRules<T>(this IRuleBuilder<T, Guid> rule) =>
        rule.NotEmpty().WithErrorCode("address_type_required").WithMessage("Address type is required.");

    public static IRuleBuilderOptions<T, string?> CareOfRules<T>(this IRuleBuilder<T, string?> rule) =>
        rule.MaximumLength(MaxCareOfLength).WithErrorCode("address_care_of_too_long").WithMessage("C/O is too long.");

    public static IRuleBuilderOptions<T, string> Street1Rules<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty().WithErrorCode("address_street1_required").WithMessage("Street is required.")
            .MaximumLength(MaxStreetLength).WithErrorCode("address_street1_too_long").WithMessage("Street is too long.");

    public static IRuleBuilderOptions<T, string?> Street2Rules<T>(this IRuleBuilder<T, string?> rule) =>
        rule.MaximumLength(MaxStreetLength).WithErrorCode("address_street2_too_long").WithMessage("Street line 2 is too long.");

    public static IRuleBuilderOptions<T, string> PostalCodeRules<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty().WithErrorCode("address_postal_code_required").WithMessage("Postal code is required.")
            .MaximumLength(MaxPostalCodeLength).WithErrorCode("address_postal_code_too_long").WithMessage("Postal code is too long.");

    public static IRuleBuilderOptions<T, string> CityRules<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty().WithErrorCode("address_city_required").WithMessage("City is required.")
            .MaximumLength(MaxCityLength).WithErrorCode("address_city_too_long").WithMessage("City is too long.");

    public static IRuleBuilderOptions<T, string?> StateRules<T>(this IRuleBuilder<T, string?> rule) =>
        rule.MaximumLength(MaxStateLength).WithErrorCode("address_state_too_long").WithMessage("State is too long.");

    public static IRuleBuilderOptions<T, string> CountryRules<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty().WithErrorCode("address_country_required").WithMessage("Country is required.")
            .MaximumLength(MaxCountryLength).WithErrorCode("address_country_too_long").WithMessage("Country is too long.");

    /// <summary>Optional text fields: trimmed, and empty becomes null.</summary>
    public static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    public static Error ToError(this ValidationResult result)
    {
        var failure = result.Errors[0];
        return Error.Validation(failure.ErrorCode, failure.ErrorMessage);
    }
}

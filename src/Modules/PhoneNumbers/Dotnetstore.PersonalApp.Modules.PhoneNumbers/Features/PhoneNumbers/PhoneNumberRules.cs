using Dotnetstore.PersonalApp.Shared.Results;
using FluentValidation;
using FluentValidation.Results;

namespace Dotnetstore.PersonalApp.Modules.PhoneNumbers.Features.PhoneNumbers;

/// <summary>
/// Shared validation rules. Each rule carries an error code (e.g. <c>phone_number_number_invalid</c>) that the UI translates via
/// the <c>Error_{code}</c> resource; the English message is only a fallback.
/// </summary>
internal static class PhoneNumberRules
{
    public const int MaxNumberLength = 30;
    public const int MinDigits = 5;
    public const int MaxDigits = 15;

    public static IRuleBuilderOptions<T, Guid> ObjectIdRules<T>(this IRuleBuilder<T, Guid> rule) =>
        rule.NotEmpty().WithErrorCode("phone_number_object_required").WithMessage("The phone number must belong to an object.");

    public static IRuleBuilderOptions<T, Guid> PhoneNumberTypeIdRules<T>(this IRuleBuilder<T, Guid> rule) =>
        rule.NotEmpty().WithErrorCode("phone_number_type_required").WithMessage("Phone number type is required.");

    public static IRuleBuilderOptions<T, string> NumberRules<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty().WithErrorCode("phone_number_number_required").WithMessage("Phone number is required.")
            .MaximumLength(MaxNumberLength).WithErrorCode("phone_number_number_too_long").WithMessage("Phone number is too long.")
            .Must(IsValidNumber).WithErrorCode("phone_number_number_invalid").WithMessage("Phone number is not valid.");

    /// <summary>Keeps an optional leading <c>+</c> and the digits, dropping spaces, hyphens, brackets and the like.</summary>
    public static string Normalize(string number)
    {
        var trimmed = number.Trim();
        var digits = new string([.. trimmed.Where(char.IsAsciiDigit)]);
        return trimmed.StartsWith('+') ? "+" + digits : digits;
    }

    public static Error ToError(this ValidationResult result)
    {
        var failure = result.Errors[0];
        return Error.Validation(failure.ErrorCode, failure.ErrorMessage);
    }

    private static bool IsValidNumber(string number)
    {
        var trimmed = number.Trim();
        var allowed = trimmed.All(c => char.IsAsciiDigit(c) || c is ' ' or '-' or '(' or ')' or '.' or '/' or '+');
        var plusOnlyFirst = !trimmed.Skip(1).Contains('+');
        var digits = trimmed.Count(char.IsAsciiDigit);
        return allowed && plusOnlyFirst && digits is >= MinDigits and <= MaxDigits;
    }
}
using Dotnetstore.PersonalApp.Shared.Results;
using FluentValidation;
using FluentValidation.Results;

namespace Dotnetstore.PersonalApp.Modules.EmailAddresses.Features.EmailAddresses;

/// <summary>
/// Shared validation rules. Each rule carries an error code (e.g. <c>email_address_email_invalid</c>) that the UI translates via
/// the <c>Error_{code}</c> resource; the English message is only a fallback.
/// </summary>
internal static class EmailAddressRules
{
    public const int MaxEmailLength = 254;

    public static IRuleBuilderOptions<T, Guid> ObjectIdRules<T>(this IRuleBuilder<T, Guid> rule) =>
        rule.NotEmpty().WithErrorCode("email_address_object_required").WithMessage("The email address must belong to an object.");

    public static IRuleBuilderOptions<T, Guid> EmailAddressTypeIdRules<T>(this IRuleBuilder<T, Guid> rule) =>
        rule.NotEmpty().WithErrorCode("email_address_type_required").WithMessage("Email address type is required.");

    public static IRuleBuilderOptions<T, string> EmailRules<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty().WithErrorCode("email_address_email_required").WithMessage("Email address is required.")
            .MaximumLength(MaxEmailLength).WithErrorCode("email_address_email_too_long").WithMessage("Email address is too long.")
            .EmailAddress().WithErrorCode("email_address_email_invalid").WithMessage("Email address is not valid.");

    public static Error ToError(this ValidationResult result)
    {
        var failure = result.Errors[0];
        return Error.Validation(failure.ErrorCode, failure.ErrorMessage);
    }
}
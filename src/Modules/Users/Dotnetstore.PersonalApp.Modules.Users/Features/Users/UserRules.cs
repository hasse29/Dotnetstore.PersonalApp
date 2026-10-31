using System.Diagnostics.CodeAnalysis;
using Dotnetstore.PersonalApp.Shared.Results;
using FluentValidation;
using FluentValidation.Results;

namespace Dotnetstore.PersonalApp.Modules.Users.Features.Users;

/// <summary>
/// Shared validation rules. Each rule carries an error code (e.g. <c>first_name_required</c>) that the UI translates via
/// the <c>Error_{code}</c> resource; the English message is only a fallback.
/// </summary>
internal static class UserRules
{
    public const int MinPasswordLength = 8;
    public const int MaxNameLength = 100;
    public const int MaxUsernameLength = 254;
    public const int MaxPasswordLength = 128;

    public static IRuleBuilderOptions<T, string> NameRules<T>(this IRuleBuilder<T, string> rule, string field) =>
        rule.NotEmpty().WithErrorCode($"{field}_required").WithMessage($"{field} is required.")
            .MaximumLength(MaxNameLength).WithErrorCode($"{field}_too_long").WithMessage($"{field} is too long.");

    public static IRuleBuilderOptions<T, string> UsernameRules<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty().WithErrorCode("username_required").WithMessage("Username is required.")
            .MaximumLength(MaxUsernameLength).WithErrorCode("username_too_long").WithMessage("Username is too long.")
            .EmailAddress().WithErrorCode("username_invalid").WithMessage("Username must be a valid email address.");

    public static IRuleBuilderOptions<T, string> PasswordRules<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty().WithErrorCode("password_required").WithMessage("Password is required.")
            .MinimumLength(MinPasswordLength).WithErrorCode("password_too_short").WithMessage("Password is too short.")
            .MaximumLength(MaxPasswordLength).WithErrorCode("password_too_long").WithMessage("Password is too long.");

    public static IRuleBuilderOptions<T, DateOnly> DateOfBirthRules<T>(this IRuleBuilder<T, DateOnly> rule, TimeProvider clock) =>
        rule.Must(d => d <= DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime))
            .WithErrorCode("date_of_birth_future").WithMessage("Date of birth cannot be in the future.")
            .Must(d => d.Year >= 1900)
            .WithErrorCode("date_of_birth_invalid").WithMessage("Date of birth is not valid.");

    // Lowercase on purpose: usernames are email addresses and are compared case-insensitively.
    [SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase", Justification = "Usernames are stored lowercase.")]
    public static string NormalizeUsername(string username) => username.Trim().ToLowerInvariant();

    public static Error ToError(this ValidationResult result)
    {
        var failure = result.Errors[0];
        return Error.Validation(failure.ErrorCode, failure.ErrorMessage);
    }
}

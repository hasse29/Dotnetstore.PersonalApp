using System.Globalization;
using Dotnetstore.PersonalApp.Shared.Results;
using FluentValidation;
using FluentValidation.Results;

namespace Dotnetstore.PersonalApp.Modules.Clients.Features.Clients;

/// <summary>
/// Shared validation rules. Each rule carries an error code (e.g. <c>client_name_required</c>) that the UI translates via
/// the <c>Error_{code}</c> resource; the English message is only a fallback.
/// </summary>
internal static class ClientRules
{
    public const int MaxNameLength = 200;
    public const int MaxOrganizationNumberLength = 20;
    public const int MaxVatNumberLength = 20;
    public const int MaxEmailLength = 254;
    public const int MaxPhoneLength = 30;
    public const int MaxWebsiteLength = 2048;
    public const int MaxNotesLength = 2000;

    public static IRuleBuilderOptions<T, string> NameRules<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty().WithErrorCode("client_name_required").WithMessage("Company name is required.")
            .MaximumLength(MaxNameLength).WithErrorCode("client_name_too_long").WithMessage("Company name is too long.");

    public static IRuleBuilderOptions<T, string> OrganizationNumberRules<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty().WithErrorCode("client_organization_number_required").WithMessage("Organization number is required.")
            .MaximumLength(MaxOrganizationNumberLength).WithErrorCode("client_organization_number_too_long").WithMessage("Organization number is too long.")
            .Must(IsValidOrganizationNumber).WithErrorCode("client_organization_number_invalid")
            .WithMessage("Organization number may only contain letters, digits and hyphens.");

    public static IRuleBuilderOptions<T, string?> VatNumberRules<T>(this IRuleBuilder<T, string?> rule) =>
        rule.MaximumLength(MaxVatNumberLength).WithErrorCode("client_vat_number_too_long").WithMessage("VAT number is too long.");

    public static IRuleBuilderOptions<T, string?> EmailRules<T>(this IRuleBuilder<T, string?> rule) =>
        rule.MaximumLength(MaxEmailLength).WithErrorCode("client_email_too_long").WithMessage("Email is too long.")
            .EmailAddress().WithErrorCode("client_email_invalid").WithMessage("Email must be a valid email address.");

    public static IRuleBuilderOptions<T, string?> PhoneRules<T>(this IRuleBuilder<T, string?> rule) =>
        rule.MaximumLength(MaxPhoneLength).WithErrorCode("client_phone_too_long").WithMessage("Phone is too long.");

    public static IRuleBuilderOptions<T, string?> WebsiteRules<T>(this IRuleBuilder<T, string?> rule) =>
        rule.MaximumLength(MaxWebsiteLength).WithErrorCode("client_website_too_long").WithMessage("Website is too long.")
            .Must(IsHttpUrl).WithErrorCode("client_website_invalid").WithMessage("Website must be a valid http or https address.");

    public static IRuleBuilderOptions<T, string?> NotesRules<T>(this IRuleBuilder<T, string?> rule) =>
        rule.MaximumLength(MaxNotesLength).WithErrorCode("client_notes_too_long").WithMessage("Notes are too long.");

    /// <summary>Organization numbers are compared ignoring case and surrounding/inner spaces.</summary>
    public static string NormalizeOrganizationNumber(string value) =>
        value.Replace(" ", string.Empty, StringComparison.Ordinal).ToUpper(CultureInfo.InvariantCulture);

    /// <summary>Optional text fields: trimmed, and empty becomes null.</summary>
    public static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    public static Error ToError(this ValidationResult result)
    {
        var failure = result.Errors[0];
        return Error.Validation(failure.ErrorCode, failure.ErrorMessage);
    }

    private static bool IsValidOrganizationNumber(string value) =>
        NormalizeOrganizationNumber(value).All(c => char.IsAsciiLetterOrDigit(c) || c == '-');

    private static bool IsHttpUrl(string? value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https";
}

using Dotnetstore.PersonalApp.Shared.Results;
using FluentValidation;
using FluentValidation.Results;

namespace Dotnetstore.PersonalApp.Modules.Cvs.Features.Cvs;

/// <summary>
/// Shared validation rules. Each rule carries an error code (e.g. <c>cv_title_required</c>) that the UI translates via the
/// <c>Error_{code}</c> resource; the English message is only a fallback.
/// </summary>
internal static class CvRules
{
    public const int MaxTitleLength = 200;
    public const int MaxTargetJobLength = 200;
    public const int MaxSummaryLength = 4000;
    public const int MaxLinkedInLength = 300;

    public static IRuleBuilderOptions<T, Guid> UserIdRules<T>(this IRuleBuilder<T, Guid> rule) =>
        rule.NotEmpty().WithErrorCode("cv_user_required").WithMessage("A CV must belong to a user.");

    public static IRuleBuilderOptions<T, string> TitleRules<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty().WithErrorCode("cv_title_required").WithMessage("Title is required.")
            .MaximumLength(MaxTitleLength).WithErrorCode("cv_title_too_long").WithMessage("Title is too long.");

    public static IRuleBuilderOptions<T, string?> TargetJobRules<T>(this IRuleBuilder<T, string?> rule) =>
        rule.MaximumLength(MaxTargetJobLength).WithErrorCode("cv_target_job_too_long").WithMessage("Target job is too long.");

    public static IRuleBuilderOptions<T, string?> SummaryRules<T>(this IRuleBuilder<T, string?> rule) =>
        rule.MaximumLength(MaxSummaryLength).WithErrorCode("cv_summary_too_long").WithMessage("Summary is too long.");

    public static IRuleBuilderOptions<T, string?> LinkedInRules<T>(this IRuleBuilder<T, string?> rule) =>
        rule.Must(value => string.IsNullOrWhiteSpace(value) || NormalizeLinkedIn(value) is not null)
            .WithErrorCode("cv_linkedin_invalid").WithMessage("The LinkedIn link is not valid.");

    /// <summary>
    /// A LinkedIn address as an https URL: "linkedin.com/in/anna" gets its scheme, and anything that is not on linkedin.com is rejected (null).
    /// Empty input gives null as well.
    /// </summary>
    public static string? NormalizeLinkedIn(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var text = value.Trim();
        if (!text.Contains("://", StringComparison.Ordinal))
        {
            text = "https://" + text;
        }

        var valid = Uri.TryCreate(text, UriKind.Absolute, out var uri)
            && uri.Scheme == Uri.UriSchemeHttps
            && (uri.Host.Equals("linkedin.com", StringComparison.OrdinalIgnoreCase) || uri.Host.EndsWith(".linkedin.com", StringComparison.OrdinalIgnoreCase))
            && text.Length <= MaxLinkedInLength;
        return valid ? uri!.AbsoluteUri : null;
    }

    /// <summary>Optional text fields: trimmed, and empty becomes null.</summary>
    public static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    public static Error ToError(this ValidationResult result)
    {
        var failure = result.Errors[0];
        return Error.Validation(failure.ErrorCode, failure.ErrorMessage);
    }
}

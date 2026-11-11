using Dotnetstore.PersonalApp.Shared.Results;
using FluentValidation;
using FluentValidation.Results;

namespace Dotnetstore.PersonalApp.Modules.Products.Features.ProductCategories;

/// <summary>
/// Shared validation rules. Each rule carries an error code (e.g. <c>product_category_name_required</c>) that the UI translates via
/// the <c>Error_{code}</c> resource; the English message is only a fallback.
/// </summary>
internal static class ProductCategoryRules
{
    public const int MaxNameLength = 100;
    public const int MaxDescriptionLength = 500;

    public static IRuleBuilderOptions<T, string> NameRules<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty().WithErrorCode("product_category_name_required").WithMessage("Name is required.")
            .MaximumLength(MaxNameLength).WithErrorCode("product_category_name_too_long").WithMessage("Name is too long.");

    public static IRuleBuilderOptions<T, string?> DescriptionRules<T>(this IRuleBuilder<T, string?> rule) =>
        rule.MaximumLength(MaxDescriptionLength).WithErrorCode("product_category_description_too_long").WithMessage("Description is too long.");

    /// <summary>Optional text fields: trimmed, and empty becomes null.</summary>
    public static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    public static Error ToError(this ValidationResult result)
    {
        var failure = result.Errors[0];
        return Error.Validation(failure.ErrorCode, failure.ErrorMessage);
    }
}

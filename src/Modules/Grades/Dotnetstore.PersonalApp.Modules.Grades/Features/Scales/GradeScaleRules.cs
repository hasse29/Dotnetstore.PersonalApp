using Dotnetstore.PersonalApp.Shared.Results;
using FluentValidation;
using FluentValidation.Results;

namespace Dotnetstore.PersonalApp.Modules.Grades.Features.Scales;

/// <summary>A step of a scale as typed in the editor; <see cref="Id"/> is set for steps that already exist so grades keep pointing at them.</summary>
internal sealed record GradeScaleItemInput(Guid? Id, string Label, int Rank, string? Description, bool IsPassing);

/// <summary>Shared validation rules; the error codes (<c>grade_*</c>) are translated through <c>Error_{code}</c>.</summary>
internal static class GradeScaleRules
{
    public const int MaxNameLength = 100;
    public const int MaxCountryLength = 100;
    public const int MaxDescriptionLength = 500;
    public const int MaxItems = 50;
    public const int MaxLabelLength = 20;
    public const int MaxItemDescriptionLength = 200;

    public static string? NormalizeOptional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    public static Error ToError(this ValidationResult result)
    {
        var failure = result.Errors[0];
        return Error.Validation(failure.ErrorCode, failure.ErrorMessage);
    }

    public static IRuleBuilderOptions<T, string> NameRules<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty().WithErrorCode("grade_scale_name_required").WithMessage("Name is required.")
            .MaximumLength(MaxNameLength).WithErrorCode("grade_scale_name_too_long").WithMessage("Name is too long.");

    public static IRuleBuilderOptions<T, string?> CountryRules<T>(this IRuleBuilder<T, string?> rule) =>
        rule.MaximumLength(MaxCountryLength).WithErrorCode("grade_scale_country_too_long").WithMessage("Country is too long.");

    public static IRuleBuilderOptions<T, string?> DescriptionRules<T>(this IRuleBuilder<T, string?> rule) =>
        rule.MaximumLength(MaxDescriptionLength).WithErrorCode("grade_scale_description_too_long").WithMessage("Description is too long.");

    public static IRuleBuilderOptions<T, IReadOnlyList<GradeScaleItemInput>> ItemsRules<T>(this IRuleBuilder<T, IReadOnlyList<GradeScaleItemInput>> rule) =>
        rule.NotEmpty().WithErrorCode("grade_scale_items_required").WithMessage("Add at least one grade.")
            .Must(items => items.Count <= MaxItems).WithErrorCode("grade_scale_too_many_items").WithMessage("Too many grades.")
            .Must(items => items.All(i => !string.IsNullOrWhiteSpace(i.Label))).WithErrorCode("grade_item_label_required").WithMessage("Every grade needs a label.")
            .Must(items => items.All(i => i.Label.Trim().Length <= MaxLabelLength)).WithErrorCode("grade_item_label_too_long").WithMessage("A label is too long.")
            .Must(items => items.All(i => (i.Description?.Trim().Length ?? 0) <= MaxItemDescriptionLength)).WithErrorCode("grade_item_description_too_long").WithMessage("A description is too long.")
            .Must(items => items.Select(i => i.Label.Trim()).Distinct(StringComparer.CurrentCultureIgnoreCase).Count() == items.Count)
            .WithErrorCode("grade_item_label_duplicate").WithMessage("Two grades have the same label.");
}

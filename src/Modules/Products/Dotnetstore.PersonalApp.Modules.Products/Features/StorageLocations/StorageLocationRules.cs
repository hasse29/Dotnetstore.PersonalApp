using Dotnetstore.PersonalApp.Modules.Products.Domain;
using Dotnetstore.PersonalApp.Shared.Results;
using FluentValidation;
using FluentValidation.Results;

namespace Dotnetstore.PersonalApp.Modules.Products.Features.StorageLocations;

/// <summary>What the create and update commands share.</summary>
internal sealed record StorageLocationFields(string Name, string? Description, bool IsActive);

/// <summary>Validation rules; the error codes (<c>storage_location_*</c>) are translated through <c>Error_{code}</c> resources.</summary>
internal static class StorageLocationRules
{
    public const int MaxNameLength = 100;
    public const int MaxDescriptionLength = 500;

    public static IRuleBuilderOptions<T, string> NameRules<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty().WithErrorCode("storage_location_name_required").WithMessage("Name is required.")
            .MaximumLength(MaxNameLength).WithErrorCode("storage_location_name_too_long").WithMessage("Name is too long.");

    public static IRuleBuilderOptions<T, string?> DescriptionRules<T>(this IRuleBuilder<T, string?> rule) =>
        rule.MaximumLength(MaxDescriptionLength).WithErrorCode("storage_location_description_too_long").WithMessage("Description is too long.");

    public static void Apply(StorageLocation location, StorageLocationFields fields)
    {
        location.Name = fields.Name.Trim();
        location.Description = string.IsNullOrWhiteSpace(fields.Description) ? null : fields.Description.Trim();
        location.IsActive = fields.IsActive;
    }

    public static Error ToError(this ValidationResult result)
    {
        var failure = result.Errors[0];
        return Error.Validation(failure.ErrorCode, failure.ErrorMessage);
    }
}

using Dotnetstore.PersonalApp.Modules.Products.Domain;
using Dotnetstore.PersonalApp.Shared.Results;
using FluentValidation;
using FluentValidation.Results;

namespace Dotnetstore.PersonalApp.Modules.Products.Features.Products;

/// <summary>What the create and update commands share.</summary>
internal sealed record ProductFields(
    string Number,
    string Name,
    ProductKind Kind,
    Guid? CategoryId,
    string? Description,
    bool IsActive,
    ProductUnit Unit,
    decimal UnitPrice,
    decimal CostPrice,
    decimal VatRate,
    string? Manufacturer,
    string? Model,
    string? SoftwareVersion,
    decimal? MinimumHours,
    decimal? MinimumStock);

/// <summary>
/// Shared validation rules. Each rule carries an error code (<c>product_*</c>) that the UI translates via the
/// <c>Error_{code}</c> resource; the English message is only a fallback.
/// </summary>
internal static class ProductRules
{
    public const int MaxNumberLength = 30;
    public const int MaxNameLength = 200;
    public const int MaxDescriptionLength = 2000;
    public const int MaxManufacturerLength = 100;
    public const int MaxModelLength = 100;
    public const int MaxVersionLength = 50;
    public const decimal MaxPrice = 10_000_000m;
    public const decimal MaxMinimumHours = 1000m;
    public const decimal MaxStock = 1_000_000m;

    public static IRuleBuilderOptions<T, string> NumberRules<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty().WithErrorCode("product_number_required").WithMessage("Product number is required.")
            .MaximumLength(MaxNumberLength).WithErrorCode("product_number_too_long").WithMessage("Product number is too long.");

    public static IRuleBuilderOptions<T, string> NameRules<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty().WithErrorCode("product_name_required").WithMessage("Name is required.")
            .MaximumLength(MaxNameLength).WithErrorCode("product_name_too_long").WithMessage("Name is too long.");

    public static IRuleBuilderOptions<T, ProductKind> KindRules<T>(this IRuleBuilder<T, ProductKind> rule) =>
        rule.IsInEnum().WithErrorCode("product_kind_invalid").WithMessage("Kind is not valid.");

    public static IRuleBuilderOptions<T, ProductUnit> UnitRules<T>(this IRuleBuilder<T, ProductUnit> rule) =>
        rule.IsInEnum().WithErrorCode("product_unit_invalid").WithMessage("Unit is not valid.");

    public static IRuleBuilderOptions<T, string?> DescriptionRules<T>(this IRuleBuilder<T, string?> rule) =>
        rule.MaximumLength(MaxDescriptionLength).WithErrorCode("product_description_too_long").WithMessage("Description is too long.");

    public static IRuleBuilderOptions<T, decimal> PriceRules<T>(this IRuleBuilder<T, decimal> rule) =>
        rule.InclusiveBetween(0m, MaxPrice).WithErrorCode("product_price_invalid").WithMessage("Price must be between 0 and 10 000 000.");

    public static IRuleBuilderOptions<T, decimal> VatRateRules<T>(this IRuleBuilder<T, decimal> rule) =>
        rule.InclusiveBetween(0m, 100m).WithErrorCode("product_vat_invalid").WithMessage("VAT must be between 0 and 100 percent.");

    public static IRuleBuilderOptions<T, string?> ManufacturerRules<T>(this IRuleBuilder<T, string?> rule) =>
        rule.MaximumLength(MaxManufacturerLength).WithErrorCode("product_manufacturer_too_long").WithMessage("Manufacturer is too long.");

    public static IRuleBuilderOptions<T, string?> ModelRules<T>(this IRuleBuilder<T, string?> rule) =>
        rule.MaximumLength(MaxModelLength).WithErrorCode("product_model_too_long").WithMessage("Model is too long.");

    public static IRuleBuilderOptions<T, string?> VersionRules<T>(this IRuleBuilder<T, string?> rule) =>
        rule.MaximumLength(MaxVersionLength).WithErrorCode("product_version_too_long").WithMessage("Version is too long.");

    public static IRuleBuilderOptions<T, decimal?> MinimumHoursRules<T>(this IRuleBuilder<T, decimal?> rule) =>
        rule.Must(h => h is null || (h > 0m && h <= MaxMinimumHours)).WithErrorCode("product_minimum_hours_invalid")
            .WithMessage("Minimum hours must be more than 0 and at most 1000.");

    public static IRuleBuilderOptions<T, decimal?> MinimumStockRules<T>(this IRuleBuilder<T, decimal?> rule) =>
        rule.Must(s => s is null || (s >= 0m && s <= MaxStock)).WithErrorCode("product_minimum_stock_invalid")
            .WithMessage("Minimum stock must be between 0 and 1 000 000.");

    /// <summary>Optional text fields: trimmed, and empty becomes null.</summary>
    public static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    public static decimal Round(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);

    /// <summary>Copies the editable fields onto the product, keeping the kind-specific ones only for the product's own kind.</summary>
    public static void Apply(Product product, ProductFields fields)
    {
        product.Number = fields.Number.Trim();
        product.Name = fields.Name.Trim();
        product.Kind = fields.Kind;
        product.CategoryId = fields.CategoryId;
        product.Description = NormalizeOptional(fields.Description);
        product.IsActive = fields.IsActive;
        product.Unit = fields.Unit;
        product.UnitPrice = Round(fields.UnitPrice);
        product.CostPrice = Round(fields.CostPrice);
        product.VatRate = Round(fields.VatRate);
        product.Manufacturer = fields.Kind == ProductKind.Hardware ? NormalizeOptional(fields.Manufacturer) : null;
        product.Model = fields.Kind == ProductKind.Hardware ? NormalizeOptional(fields.Model) : null;
        product.SoftwareVersion = fields.Kind == ProductKind.Software ? NormalizeOptional(fields.SoftwareVersion) : null;
        product.MinimumHours = fields.Kind == ProductKind.Consulting && fields.MinimumHours is { } hours ? Round(hours) : null;
        product.MinimumStock = fields.Kind != ProductKind.Consulting && fields.MinimumStock is { } stock ? Round(stock) : null;
    }

    public static Error ToError(this ValidationResult result)
    {
        var failure = result.Errors[0];
        return Error.Validation(failure.ErrorCode, failure.ErrorMessage);
    }
}

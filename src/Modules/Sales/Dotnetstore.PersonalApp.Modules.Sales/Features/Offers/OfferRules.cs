using Dotnetstore.PersonalApp.Modules.Sales.Domain;
using Dotnetstore.PersonalApp.Shared.Results;
using FluentValidation;
using FluentValidation.Results;

namespace Dotnetstore.PersonalApp.Modules.Sales.Features.Offers;

internal sealed record OfferLineFields(
    Guid? ProductId,
    string Description,
    decimal Quantity,
    string Unit,
    decimal UnitPrice,
    decimal VatRate,
    decimal DiscountPercent);

/// <summary>What the create and update commands share.</summary>
internal sealed record OfferFields(
    Guid CustomerId,
    DateOnly Date,
    DateOnly ValidUntil,
    string? Reference,
    string? Notes,
    IReadOnlyList<OfferLineFields> Lines);

/// <summary>
/// Shared validation rules. Each rule carries an error code (<c>offer_*</c>) that the UI translates via the
/// <c>Error_{code}</c> resource; the English message is only a fallback.
/// </summary>
internal static class OfferRules
{
    public const int MaxReferenceLength = 200;
    public const int MaxNotesLength = 2000;
    public const int MaxDescriptionLength = 300;
    public const int MaxUnitLength = 20;
    public const int MaxLines = 200;
    public const decimal MaxQuantity = 1_000_000m;
    public const decimal MaxPrice = 10_000_000m;

    public static void Apply(Offer offer, OfferFields fields)
    {
        offer.Date = fields.Date;
        offer.ValidUntil = fields.ValidUntil;
        offer.Reference = Normalize(fields.Reference);
        offer.Notes = Normalize(fields.Notes);
    }

    public static OfferLine ToLine(OfferLineFields fields, int position) => new()
    {
        Id = Guid.CreateVersion7(),
        Position = position,
        ProductId = fields.ProductId,
        Description = fields.Description.Trim(),
        Quantity = Math.Round(fields.Quantity, 2, MidpointRounding.AwayFromZero),
        Unit = fields.Unit.Trim(),
        UnitPrice = Math.Round(fields.UnitPrice, 2, MidpointRounding.AwayFromZero),
        VatRate = Math.Round(fields.VatRate, 2, MidpointRounding.AwayFromZero),
        DiscountPercent = Math.Round(fields.DiscountPercent, 2, MidpointRounding.AwayFromZero),
    };

    public static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    public static Error ToError(this ValidationResult result)
    {
        var failure = result.Errors[0];
        return Error.Validation(failure.ErrorCode, failure.ErrorMessage);
    }
}

internal sealed class OfferFieldsValidator : AbstractValidator<OfferFields>
{
    public OfferFieldsValidator()
    {
        RuleFor(f => f.CustomerId).NotEmpty().WithErrorCode("offer_customer_required").WithMessage("Select a customer.");
        RuleFor(f => f.ValidUntil).Must((f, validUntil) => validUntil >= f.Date)
            .WithErrorCode("offer_valid_until_before_date").WithMessage("The offer cannot be valid until before its date.");
        RuleFor(f => f.Reference).MaximumLength(OfferRules.MaxReferenceLength)
            .WithErrorCode("offer_reference_too_long").WithMessage("Reference is too long.");
        RuleFor(f => f.Notes).MaximumLength(OfferRules.MaxNotesLength)
            .WithErrorCode("offer_notes_too_long").WithMessage("Notes are too long.");
        RuleFor(f => f.Lines.Count).LessThanOrEqualTo(OfferRules.MaxLines)
            .WithErrorCode("offer_too_many_lines").WithMessage("There are too many lines.");

        RuleForEach(f => f.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.Description).NotEmpty().WithErrorCode("offer_line_description_required").WithMessage("Describe the line.")
                .MaximumLength(OfferRules.MaxDescriptionLength).WithErrorCode("offer_line_description_too_long").WithMessage("Description is too long.");
            line.RuleFor(l => l.Unit).NotEmpty().WithErrorCode("offer_line_unit_required").WithMessage("Unit is required.")
                .MaximumLength(OfferRules.MaxUnitLength).WithErrorCode("offer_line_unit_required").WithMessage("Unit is not valid.");
            line.RuleFor(l => l.Quantity).Must(q => q > 0 && q <= OfferRules.MaxQuantity)
                .WithErrorCode("offer_line_quantity_invalid").WithMessage("Quantity must be more than 0.");
            line.RuleFor(l => l.UnitPrice).Must(p => p >= 0 && p <= OfferRules.MaxPrice)
                .WithErrorCode("offer_line_price_invalid").WithMessage("Price is not valid.");
            line.RuleFor(l => l.VatRate).Must(v => v is >= 0 and <= 100)
                .WithErrorCode("offer_line_vat_invalid").WithMessage("VAT must be between 0 and 100.");
            line.RuleFor(l => l.DiscountPercent).Must(d => d is >= 0 and <= 100)
                .WithErrorCode("offer_line_discount_invalid").WithMessage("Discount must be between 0 and 100.");
        });
    }
}

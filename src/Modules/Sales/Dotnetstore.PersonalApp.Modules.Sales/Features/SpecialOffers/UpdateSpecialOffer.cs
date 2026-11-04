using Dotnetstore.PersonalApp.Modules.Sales.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Sales.Features.SpecialOffers;

internal static class UpdateSpecialOffer
{
    public sealed record Command(Guid Id, string Description, decimal DiscountPct, string OfferType, string Category, DateOnly StartDate, DateOnly EndDate, int MinQty, int? MaxQty);

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(c => c.Description).NotEmpty().WithErrorCode("special_offer_description_required").WithMessage("Description is required.").MaximumLength(255).WithErrorCode("special_offer_description_too_long").WithMessage("Description is too long.");
            RuleFor(c => c.DiscountPct).InclusiveBetween(0m, 1m).WithErrorCode("special_offer_discount_pct_out_of_range").WithMessage("Discount (0-1) is out of range.");
            RuleFor(c => c.OfferType).NotEmpty().WithErrorCode("special_offer_offer_type_required").WithMessage("Type is required.").MaximumLength(50).WithErrorCode("special_offer_offer_type_too_long").WithMessage("Type is too long.");
            RuleFor(c => c.Category).NotEmpty().WithErrorCode("special_offer_category_required").WithMessage("Category is required.").MaximumLength(50).WithErrorCode("special_offer_category_too_long").WithMessage("Category is too long.");
            RuleFor(c => c.MinQty).InclusiveBetween(0, 1000000).WithErrorCode("special_offer_min_qty_out_of_range").WithMessage("Minimum quantity is out of range.");
            RuleFor(c => c.MaxQty).InclusiveBetween(0, 1000000).WithErrorCode("special_offer_max_qty_out_of_range").WithMessage("Maximum quantity is out of range.").When(c => c.MaxQty is not null);
            RuleFor(c => c).Must(c => c.EndDate >= c.StartDate).WithErrorCode("special_offer_end_before_start").WithMessage("The end date is before the start date.");
        }
    }

    public sealed class Handler(IDbContextFactory<SalesDbContext> dbFactory, IValidator<Command> validator)
    {
        public async Task<Result<SpecialOfferDto>> HandleAsync(Command command, CancellationToken cancellationToken)
        {
            var validation = await validator.ValidateAsync(command, cancellationToken).ConfigureAwait(false);
            if (!validation.IsValid)
            {
                return validation.ToError();
            }

            var descriptionValue = command.Description.Trim();
            var offerTypeValue = command.OfferType.Trim();
            var categoryValue = command.Category.Trim();

            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            var entity = await db.SpecialOffers.FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken).ConfigureAwait(false);
            if (entity is null)
            {
                return Error.NotFound("special_offer_not_found", "The record was not found.");
            }



            if (await db.SpecialOffers.AnyAsync(x => x.Description == descriptionValue && x.Id != command.Id, cancellationToken).ConfigureAwait(false))
            {
                return Error.Conflict("special_offer_conflict", "A special offer with the same values already exists.");
            }

            entity.Description = descriptionValue;
            entity.DiscountPct = command.DiscountPct;
            entity.OfferType = offerTypeValue;
            entity.Category = categoryValue;
            entity.StartDate = command.StartDate;
            entity.EndDate = command.EndDate;
            entity.MinQty = command.MinQty;
            entity.MaxQty = command.MaxQty;

            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return await db.SpecialOffers.AsNoTracking()
                .Where(x => x.Id == entity.Id)
                .Select(SpecialOfferDto.Projection)
                .FirstAsync(cancellationToken)
                .ConfigureAwait(false);
        }
    }
}
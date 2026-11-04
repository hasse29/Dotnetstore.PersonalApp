using Dotnetstore.PersonalApp.Modules.Sales.Domain;
using Dotnetstore.PersonalApp.Modules.Sales.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Sales.Features.SpecialOfferProducts;

internal static class CreateSpecialOfferProduct
{
    public sealed record Command(Guid SpecialOfferId, Guid ProductId);

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(c => c.SpecialOfferId).NotEqual(Guid.Empty).WithErrorCode("special_offer_product_special_offer_required").WithMessage("Special offer is required.");
            RuleFor(c => c.ProductId).NotEqual(Guid.Empty).WithErrorCode("special_offer_product_product_required").WithMessage("Product is required.");
        }
    }

    public sealed class Handler(IDbContextFactory<SalesDbContext> dbFactory, IValidator<Command> validator, ExternalReferences references)
    {
        public async Task<Result<SpecialOfferProductDto>> HandleAsync(Command command, CancellationToken cancellationToken)
        {
            var validation = await validator.ValidateAsync(command, cancellationToken).ConfigureAwait(false);
            if (!validation.IsValid)
            {
                return validation.ToError();
            }



            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);


            if (!await db.SpecialOffers.AnyAsync(x => x.Id == command.SpecialOfferId, cancellationToken).ConfigureAwait(false))
            {
                return Error.NotFound("special_offer_product_special_offer_not_found", "Special offer not found.");
            }

            if (!await references.ProductExistsAsync(command.ProductId, cancellationToken).ConfigureAwait(false))
            {
                return Error.NotFound("special_offer_product_product_not_found", "Product not found.");
            }

            if (await db.SpecialOfferProducts.AnyAsync(x => x.SpecialOfferId == command.SpecialOfferId && x.ProductId == command.ProductId, cancellationToken).ConfigureAwait(false))
            {
                return Error.Conflict("special_offer_product_conflict", "A offer product with the same values already exists.");
            }

            var entity = new SpecialOfferProduct
            {
                Id = Guid.CreateVersion7(),
                SpecialOfferId = command.SpecialOfferId,
                ProductId = command.ProductId,
            };

            db.SpecialOfferProducts.Add(entity);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            var dto = await db.SpecialOfferProducts.AsNoTracking()
                .Where(x => x.Id == entity.Id)
                .Select(SpecialOfferProductDto.Projection)
                .FirstAsync(cancellationToken)
                .ConfigureAwait(false);

            return dto.WithNames(await references.ProductNamesAsync(cancellationToken).ConfigureAwait(false));
        }
    }
}
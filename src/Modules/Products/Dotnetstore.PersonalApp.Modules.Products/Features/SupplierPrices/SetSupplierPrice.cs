using Dotnetstore.PersonalApp.Modules.Products.Domain;
using Dotnetstore.PersonalApp.Modules.Products.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Products.Features.SupplierPrices;

/// <summary>Adds a supplier's price for a product, or replaces it when that supplier already has one.</summary>
internal static class SetSupplierPrice
{
    public const int MaxArticleNumberLength = 50;
    public const decimal MaxPrice = 10_000_000m;

    public sealed record Command(Guid ProductId, Guid SupplierId, string? SupplierArticleNumber, decimal Price);

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(c => c.Price).InclusiveBetween(0m, MaxPrice)
                .WithErrorCode("supplier_price_invalid").WithMessage("Price must be between 0 and 10 000 000.");
            RuleFor(c => c.SupplierArticleNumber).MaximumLength(MaxArticleNumberLength)
                .WithErrorCode("supplier_article_number_too_long").WithMessage("Article number is too long.");
        }
    }

    public sealed class Handler(IDbContextFactory<ProductsDbContext> dbFactory, IValidator<Command> validator, TimeProvider clock)
    {
        public async Task<Result<SupplierPriceDto>> HandleAsync(Command command, CancellationToken cancellationToken)
        {
            var validation = await validator.ValidateAsync(command, cancellationToken).ConfigureAwait(false);
            if (!validation.IsValid)
            {
                var failure = validation.Errors[0];
                return Error.Validation(failure.ErrorCode, failure.ErrorMessage);
            }

            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            if (!await db.Products.AnyAsync(p => p.Id == command.ProductId, cancellationToken).ConfigureAwait(false))
            {
                return Error.NotFound("product_not_found", "Product not found.");
            }

            var supplier = await db.Suppliers.AsNoTracking()
                .Where(s => s.Id == command.SupplierId)
                .Select(s => new { s.Name, s.IsActive })
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);
            if (supplier is null)
            {
                return Error.NotFound("supplier_not_found", "Supplier not found.");
            }

            var price = await db.SupplierPrices
                .FirstOrDefaultAsync(p => p.ProductId == command.ProductId && p.SupplierId == command.SupplierId, cancellationToken)
                .ConfigureAwait(false);
            if (price is null)
            {
                price = new SupplierPrice { Id = Guid.CreateVersion7(), ProductId = command.ProductId, SupplierId = command.SupplierId };
                db.SupplierPrices.Add(price);
            }

            price.SupplierArticleNumber = string.IsNullOrWhiteSpace(command.SupplierArticleNumber) ? null : command.SupplierArticleNumber.Trim();
            price.Price = Math.Round(command.Price, 2, MidpointRounding.AwayFromZero);
            price.UpdatedAt = clock.GetUtcNow();

            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return new SupplierPriceDto(
                price.Id, price.SupplierId, supplier.Name, supplier.IsActive, price.ProductId, price.SupplierArticleNumber, price.Price, price.UpdatedAt);
        }
    }
}

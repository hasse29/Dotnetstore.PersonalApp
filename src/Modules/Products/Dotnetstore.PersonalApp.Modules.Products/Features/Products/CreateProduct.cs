using Dotnetstore.PersonalApp.Modules.Products.Domain;
using Dotnetstore.PersonalApp.Modules.Products.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Products.Features.Products;

internal static class CreateProduct
{
    public sealed record Command(ProductFields Fields);

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(c => c.Fields.Number).NumberRules();
            RuleFor(c => c.Fields.Name).NameRules();
            RuleFor(c => c.Fields.Kind).KindRules();
            RuleFor(c => c.Fields.Unit).UnitRules();
            RuleFor(c => c.Fields.Description).DescriptionRules();
            RuleFor(c => c.Fields.UnitPrice).PriceRules();
            RuleFor(c => c.Fields.CostPrice).PriceRules();
            RuleFor(c => c.Fields.VatRate).VatRateRules();
            RuleFor(c => c.Fields.Manufacturer).ManufacturerRules();
            RuleFor(c => c.Fields.Model).ModelRules();
            RuleFor(c => c.Fields.SoftwareVersion).VersionRules();
            RuleFor(c => c.Fields.MinimumHours).MinimumHoursRules();
            RuleFor(c => c.Fields.MinimumStock).MinimumStockRules();
        }
    }

    public sealed class Handler(IDbContextFactory<ProductsDbContext> dbFactory, IValidator<Command> validator, TimeProvider clock)
    {
        public async Task<Result<ProductDto>> HandleAsync(Command command, CancellationToken cancellationToken)
        {
            var validation = await validator.ValidateAsync(command, cancellationToken).ConfigureAwait(false);
            if (!validation.IsValid)
            {
                return validation.ToError();
            }

            var number = command.Fields.Number.Trim();

            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            if (await db.Products.AnyAsync(p => p.Number == number, cancellationToken).ConfigureAwait(false))
            {
                return Error.Conflict("product_number_conflict", $"A product with number '{number}' already exists.");
            }

            string? categoryName = null;
            if (command.Fields.CategoryId is { } categoryId)
            {
                categoryName = await db.ProductCategories.AsNoTracking()
                    .Where(c => c.Id == categoryId)
                    .Select(c => c.Name)
                    .FirstOrDefaultAsync(cancellationToken)
                    .ConfigureAwait(false);
                if (categoryName is null)
                {
                    return Error.NotFound("product_category_not_found", "Product category not found.");
                }
            }

            var product = new Product { Id = Guid.CreateVersion7(), CreatedAt = clock.GetUtcNow() };
            ProductRules.Apply(product, command.Fields);

            db.Products.Add(product);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return ProductDto.From(product, categoryName);
        }
    }
}

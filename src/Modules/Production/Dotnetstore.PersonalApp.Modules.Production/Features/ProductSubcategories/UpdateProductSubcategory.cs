using Dotnetstore.PersonalApp.Modules.Production.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Production.Features.ProductSubcategories;

internal static class UpdateProductSubcategory
{
    public sealed record Command(Guid Id, Guid CategoryId, string Name);

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(c => c.CategoryId).NotEqual(Guid.Empty).WithErrorCode("product_subcategory_category_required").WithMessage("Category is required.");
            RuleFor(c => c.Name).NotEmpty().WithErrorCode("product_subcategory_name_required").WithMessage("Name is required.").MaximumLength(50).WithErrorCode("product_subcategory_name_too_long").WithMessage("Name is too long.");
        }
    }

    public sealed class Handler(IDbContextFactory<ProductionDbContext> dbFactory, IValidator<Command> validator)
    {
        public async Task<Result<ProductSubcategoryDto>> HandleAsync(Command command, CancellationToken cancellationToken)
        {
            var validation = await validator.ValidateAsync(command, cancellationToken).ConfigureAwait(false);
            if (!validation.IsValid)
            {
                return validation.ToError();
            }

            var nameValue = command.Name.Trim();

            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            var entity = await db.ProductSubcategories.FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken).ConfigureAwait(false);
            if (entity is null)
            {
                return Error.NotFound("product_subcategory_not_found", "The record was not found.");
            }

            if (!await db.ProductCategories.AnyAsync(x => x.Id == command.CategoryId, cancellationToken).ConfigureAwait(false))
            {
                return Error.NotFound("product_subcategory_category_not_found", "Category not found.");
            }

            if (await db.ProductSubcategories.AnyAsync(x => x.CategoryId == command.CategoryId && x.Name == nameValue && x.Id != command.Id, cancellationToken).ConfigureAwait(false))
            {
                return Error.Conflict("product_subcategory_conflict", "A product subcategory with the same values already exists.");
            }

            entity.CategoryId = command.CategoryId;
            entity.Name = nameValue;

            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return await db.ProductSubcategories.AsNoTracking()
                .Where(x => x.Id == entity.Id)
                .Select(ProductSubcategoryDto.Projection)
                .FirstAsync(cancellationToken)
                .ConfigureAwait(false);
        }
    }
}
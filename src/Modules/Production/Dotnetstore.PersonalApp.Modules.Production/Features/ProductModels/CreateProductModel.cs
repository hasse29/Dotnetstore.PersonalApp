using Dotnetstore.PersonalApp.Modules.Production.Domain;
using Dotnetstore.PersonalApp.Modules.Production.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Production.Features.ProductModels;

internal static class CreateProductModel
{
    public sealed record Command(string Name, string? Description, string? Instructions);

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(c => c.Name).NotEmpty().WithErrorCode("product_model_name_required").WithMessage("Name is required.").MaximumLength(50).WithErrorCode("product_model_name_too_long").WithMessage("Name is too long.");
            RuleFor(c => c.Description).MaximumLength(400).WithErrorCode("product_model_description_too_long").WithMessage("Description is too long.");
            RuleFor(c => c.Instructions).MaximumLength(4000).WithErrorCode("product_model_instructions_too_long").WithMessage("Instructions is too long.");
        }
    }

    public sealed class Handler(IDbContextFactory<ProductionDbContext> dbFactory, IValidator<Command> validator)
    {
        public async Task<Result<ProductModelDto>> HandleAsync(Command command, CancellationToken cancellationToken)
        {
            var validation = await validator.ValidateAsync(command, cancellationToken).ConfigureAwait(false);
            if (!validation.IsValid)
            {
                return validation.ToError();
            }

            var nameValue = command.Name.Trim();
            var descriptionValue = ProductionRules.NormalizeOptional(command.Description);
            var instructionsValue = ProductionRules.NormalizeOptional(command.Instructions);

            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);




            if (await db.ProductModels.AnyAsync(x => x.Name == nameValue, cancellationToken).ConfigureAwait(false))
            {
                return Error.Conflict("product_model_conflict", "A product model with the same values already exists.");
            }

            var entity = new ProductModel
            {
                Id = Guid.CreateVersion7(),
                Name = nameValue,
                Description = descriptionValue,
                Instructions = instructionsValue,
            };

            db.ProductModels.Add(entity);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return await db.ProductModels.AsNoTracking()
                .Where(x => x.Id == entity.Id)
                .Select(ProductModelDto.Projection)
                .FirstAsync(cancellationToken)
                .ConfigureAwait(false);
        }
    }
}
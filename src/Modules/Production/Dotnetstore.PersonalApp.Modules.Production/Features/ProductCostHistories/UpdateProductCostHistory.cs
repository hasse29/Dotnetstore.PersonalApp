using Dotnetstore.PersonalApp.Modules.Production.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Production.Features.ProductCostHistories;

internal static class UpdateProductCostHistory
{
    public sealed record Command(Guid Id, Guid ProductId, DateOnly StartDate, DateOnly? EndDate, decimal StandardCost);

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(c => c.ProductId).NotEqual(Guid.Empty).WithErrorCode("product_cost_history_product_required").WithMessage("Product is required.");
            RuleFor(c => c.StandardCost).InclusiveBetween(0m, 1000000000m).WithErrorCode("product_cost_history_standard_cost_out_of_range").WithMessage("Standard cost is out of range.");
            RuleFor(c => c).Must(c => c.EndDate is null || c.EndDate >= c.StartDate).WithErrorCode("product_cost_history_end_before_start").WithMessage("The end date is before the start date.");
        }
    }

    public sealed class Handler(IDbContextFactory<ProductionDbContext> dbFactory, IValidator<Command> validator)
    {
        public async Task<Result<ProductCostHistoryDto>> HandleAsync(Command command, CancellationToken cancellationToken)
        {
            var validation = await validator.ValidateAsync(command, cancellationToken).ConfigureAwait(false);
            if (!validation.IsValid)
            {
                return validation.ToError();
            }



            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            var entity = await db.ProductCostHistories.FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken).ConfigureAwait(false);
            if (entity is null)
            {
                return Error.NotFound("product_cost_history_not_found", "The record was not found.");
            }

            if (!await db.Products.AnyAsync(x => x.Id == command.ProductId, cancellationToken).ConfigureAwait(false))
            {
                return Error.NotFound("product_cost_history_product_not_found", "Product not found.");
            }



            entity.ProductId = command.ProductId;
            entity.StartDate = command.StartDate;
            entity.EndDate = command.EndDate;
            entity.StandardCost = command.StandardCost;

            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return await db.ProductCostHistories.AsNoTracking()
                .Where(x => x.Id == entity.Id)
                .Select(ProductCostHistoryDto.Projection)
                .FirstAsync(cancellationToken)
                .ConfigureAwait(false);
        }
    }
}
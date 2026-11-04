using Dotnetstore.PersonalApp.Modules.Production.Domain;
using Dotnetstore.PersonalApp.Modules.Production.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Production.Features.Products;

internal static class CreateProduct
{
    public sealed record Command(string Name, string ProductNumber, bool MakeFlag, bool FinishedGoodsFlag, string? Color, int SafetyStockLevel, int ReorderPoint, decimal StandardCost, decimal ListPrice, string? Size, Guid? SizeUnitId, decimal? Weight, Guid? WeightUnitId, int DaysToManufacture, string ProductLine, string Class, string Style, Guid? SubcategoryId, Guid? ModelId, DateOnly SellStartDate, DateOnly? SellEndDate, DateOnly? DiscontinuedDate);

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(c => c.Name).NotEmpty().WithErrorCode("product_name_required").WithMessage("Name is required.").MaximumLength(50).WithErrorCode("product_name_too_long").WithMessage("Name is too long.");
            RuleFor(c => c.ProductNumber).NotEmpty().WithErrorCode("product_product_number_required").WithMessage("Product number is required.").MaximumLength(25).WithErrorCode("product_product_number_too_long").WithMessage("Product number is too long.");
            RuleFor(c => c.Color).MaximumLength(15).WithErrorCode("product_color_too_long").WithMessage("Color is too long.");
            RuleFor(c => c.SafetyStockLevel).InclusiveBetween(0, 1000000).WithErrorCode("product_safety_stock_level_out_of_range").WithMessage("Safety stock level is out of range.");
            RuleFor(c => c.ReorderPoint).InclusiveBetween(0, 1000000).WithErrorCode("product_reorder_point_out_of_range").WithMessage("Reorder point is out of range.");
            RuleFor(c => c.StandardCost).InclusiveBetween(0m, 1000000000m).WithErrorCode("product_standard_cost_out_of_range").WithMessage("Standard cost is out of range.");
            RuleFor(c => c.ListPrice).InclusiveBetween(0m, 1000000000m).WithErrorCode("product_list_price_out_of_range").WithMessage("List price is out of range.");
            RuleFor(c => c.Size).MaximumLength(5).WithErrorCode("product_size_too_long").WithMessage("Size is too long.");
            RuleFor(c => c.Weight).InclusiveBetween(0m, 1000000000m).WithErrorCode("product_weight_out_of_range").WithMessage("Weight is out of range.").When(c => c.Weight is not null);
            RuleFor(c => c.DaysToManufacture).InclusiveBetween(0, 365).WithErrorCode("product_days_to_manufacture_out_of_range").WithMessage("Days to manufacture is out of range.");
            RuleFor(c => c.ProductLine).Must(v => v is "N" or "R" or "M" or "T" or "S").WithErrorCode("product_product_line_invalid").WithMessage("Product line is not valid.");
            RuleFor(c => c.Class).Must(v => v is "N" or "H" or "M" or "L").WithErrorCode("product_class_invalid").WithMessage("Class is not valid.");
            RuleFor(c => c.Style).Must(v => v is "N" or "W" or "M" or "U").WithErrorCode("product_style_invalid").WithMessage("Style is not valid.");
            RuleFor(c => c).Must(c => c.SellEndDate is null || c.SellEndDate >= c.SellStartDate).WithErrorCode("product_end_before_start").WithMessage("The end date is before the start date.");
        }
    }

    public sealed class Handler(IDbContextFactory<ProductionDbContext> dbFactory, IValidator<Command> validator)
    {
        public async Task<Result<ProductDto>> HandleAsync(Command command, CancellationToken cancellationToken)
        {
            var validation = await validator.ValidateAsync(command, cancellationToken).ConfigureAwait(false);
            if (!validation.IsValid)
            {
                return validation.ToError();
            }

            var nameValue = command.Name.Trim();
            var productNumberValue = command.ProductNumber.Trim();
            var colorValue = ProductionRules.NormalizeOptional(command.Color);
            var sizeValue = ProductionRules.NormalizeOptional(command.Size);

            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);


            if (command.SizeUnitId is { } sizeUnitId && !await db.UnitMeasures.AnyAsync(x => x.Id == sizeUnitId, cancellationToken).ConfigureAwait(false))
            {
                return Error.NotFound("product_size_unit_not_found", "Size unit not found.");
            }

            if (command.WeightUnitId is { } weightUnitId && !await db.UnitMeasures.AnyAsync(x => x.Id == weightUnitId, cancellationToken).ConfigureAwait(false))
            {
                return Error.NotFound("product_weight_unit_not_found", "Weight unit not found.");
            }

            if (command.SubcategoryId is { } subcategoryId && !await db.ProductSubcategories.AnyAsync(x => x.Id == subcategoryId, cancellationToken).ConfigureAwait(false))
            {
                return Error.NotFound("product_subcategory_not_found", "Subcategory not found.");
            }

            if (command.ModelId is { } modelId && !await db.ProductModels.AnyAsync(x => x.Id == modelId, cancellationToken).ConfigureAwait(false))
            {
                return Error.NotFound("product_model_not_found", "Model not found.");
            }

            if (await db.Products.AnyAsync(x => x.Name == nameValue, cancellationToken).ConfigureAwait(false))
            {
                return Error.Conflict("product_conflict", "A product with the same values already exists.");
            }

            if (await db.Products.AnyAsync(x => x.ProductNumber == productNumberValue, cancellationToken).ConfigureAwait(false))
            {
                return Error.Conflict("product_conflict", "A product with the same values already exists.");
            }

            var entity = new Product
            {
                Id = Guid.CreateVersion7(),
                Name = nameValue,
                ProductNumber = productNumberValue,
                MakeFlag = command.MakeFlag,
                FinishedGoodsFlag = command.FinishedGoodsFlag,
                Color = colorValue,
                SafetyStockLevel = command.SafetyStockLevel,
                ReorderPoint = command.ReorderPoint,
                StandardCost = command.StandardCost,
                ListPrice = command.ListPrice,
                Size = sizeValue,
                SizeUnitId = command.SizeUnitId,
                Weight = command.Weight,
                WeightUnitId = command.WeightUnitId,
                DaysToManufacture = command.DaysToManufacture,
                ProductLine = command.ProductLine,
                Class = command.Class,
                Style = command.Style,
                SubcategoryId = command.SubcategoryId,
                ModelId = command.ModelId,
                SellStartDate = command.SellStartDate,
                SellEndDate = command.SellEndDate,
                DiscontinuedDate = command.DiscontinuedDate,
            };

            db.Products.Add(entity);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return await db.Products.AsNoTracking()
                .Where(x => x.Id == entity.Id)
                .Select(ProductDto.Projection)
                .FirstAsync(cancellationToken)
                .ConfigureAwait(false);
        }
    }
}
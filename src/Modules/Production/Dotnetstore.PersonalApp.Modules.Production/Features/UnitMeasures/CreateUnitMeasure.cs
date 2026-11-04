using Dotnetstore.PersonalApp.Modules.Production.Domain;
using Dotnetstore.PersonalApp.Modules.Production.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Production.Features.UnitMeasures;

internal static class CreateUnitMeasure
{
    public sealed record Command(string Code, string Name);

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(c => c.Code).NotEmpty().WithErrorCode("unit_measure_code_required").WithMessage("Code is required.").MaximumLength(10).WithErrorCode("unit_measure_code_too_long").WithMessage("Code is too long.");
            RuleFor(c => c.Name).NotEmpty().WithErrorCode("unit_measure_name_required").WithMessage("Name is required.").MaximumLength(50).WithErrorCode("unit_measure_name_too_long").WithMessage("Name is too long.");
        }
    }

    public sealed class Handler(IDbContextFactory<ProductionDbContext> dbFactory, IValidator<Command> validator)
    {
        public async Task<Result<UnitMeasureDto>> HandleAsync(Command command, CancellationToken cancellationToken)
        {
            var validation = await validator.ValidateAsync(command, cancellationToken).ConfigureAwait(false);
            if (!validation.IsValid)
            {
                return validation.ToError();
            }

            var codeValue = command.Code.Trim();
            var nameValue = command.Name.Trim();

            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);




            if (await db.UnitMeasures.AnyAsync(x => x.Code == codeValue, cancellationToken).ConfigureAwait(false))
            {
                return Error.Conflict("unit_measure_conflict", "A unit of measure with the same values already exists.");
            }

            if (await db.UnitMeasures.AnyAsync(x => x.Name == nameValue, cancellationToken).ConfigureAwait(false))
            {
                return Error.Conflict("unit_measure_conflict", "A unit of measure with the same values already exists.");
            }

            var entity = new UnitMeasure
            {
                Id = Guid.CreateVersion7(),
                Code = codeValue,
                Name = nameValue,
            };

            db.UnitMeasures.Add(entity);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return await db.UnitMeasures.AsNoTracking()
                .Where(x => x.Id == entity.Id)
                .Select(UnitMeasureDto.Projection)
                .FirstAsync(cancellationToken)
                .ConfigureAwait(false);
        }
    }
}
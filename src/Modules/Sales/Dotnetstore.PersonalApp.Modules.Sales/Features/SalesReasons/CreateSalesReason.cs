using Dotnetstore.PersonalApp.Modules.Sales.Domain;
using Dotnetstore.PersonalApp.Modules.Sales.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Sales.Features.SalesReasons;

internal static class CreateSalesReason
{
    public sealed record Command(string Name, string ReasonType);

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(c => c.Name).NotEmpty().WithErrorCode("sales_reason_name_required").WithMessage("Name is required.").MaximumLength(50).WithErrorCode("sales_reason_name_too_long").WithMessage("Name is too long.");
            RuleFor(c => c.ReasonType).Must(v => v is "M" or "P" or "O").WithErrorCode("sales_reason_reason_type_invalid").WithMessage("Type is not valid.");
        }
    }

    public sealed class Handler(IDbContextFactory<SalesDbContext> dbFactory, IValidator<Command> validator)
    {
        public async Task<Result<SalesReasonDto>> HandleAsync(Command command, CancellationToken cancellationToken)
        {
            var validation = await validator.ValidateAsync(command, cancellationToken).ConfigureAwait(false);
            if (!validation.IsValid)
            {
                return validation.ToError();
            }

            var nameValue = command.Name.Trim();

            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);




            if (await db.SalesReasons.AnyAsync(x => x.Name == nameValue, cancellationToken).ConfigureAwait(false))
            {
                return Error.Conflict("sales_reason_conflict", "A sales reason with the same values already exists.");
            }

            var entity = new SalesReason
            {
                Id = Guid.CreateVersion7(),
                Name = nameValue,
                ReasonType = command.ReasonType,
            };

            db.SalesReasons.Add(entity);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return await db.SalesReasons.AsNoTracking()
                .Where(x => x.Id == entity.Id)
                .Select(SalesReasonDto.Projection)
                .FirstAsync(cancellationToken)
                .ConfigureAwait(false);
        }
    }
}
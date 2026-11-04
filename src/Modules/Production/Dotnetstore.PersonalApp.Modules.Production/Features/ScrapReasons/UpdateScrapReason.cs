using Dotnetstore.PersonalApp.Modules.Production.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Production.Features.ScrapReasons;

internal static class UpdateScrapReason
{
    public sealed record Command(Guid Id, string Name);

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(c => c.Name).NotEmpty().WithErrorCode("scrap_reason_name_required").WithMessage("Name is required.").MaximumLength(50).WithErrorCode("scrap_reason_name_too_long").WithMessage("Name is too long.");
        }
    }

    public sealed class Handler(IDbContextFactory<ProductionDbContext> dbFactory, IValidator<Command> validator)
    {
        public async Task<Result<ScrapReasonDto>> HandleAsync(Command command, CancellationToken cancellationToken)
        {
            var validation = await validator.ValidateAsync(command, cancellationToken).ConfigureAwait(false);
            if (!validation.IsValid)
            {
                return validation.ToError();
            }

            var nameValue = command.Name.Trim();

            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            var entity = await db.ScrapReasons.FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken).ConfigureAwait(false);
            if (entity is null)
            {
                return Error.NotFound("scrap_reason_not_found", "The record was not found.");
            }



            if (await db.ScrapReasons.AnyAsync(x => x.Name == nameValue && x.Id != command.Id, cancellationToken).ConfigureAwait(false))
            {
                return Error.Conflict("scrap_reason_conflict", "A scrap reason with the same values already exists.");
            }

            entity.Name = nameValue;

            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return await db.ScrapReasons.AsNoTracking()
                .Where(x => x.Id == entity.Id)
                .Select(ScrapReasonDto.Projection)
                .FirstAsync(cancellationToken)
                .ConfigureAwait(false);
        }
    }
}
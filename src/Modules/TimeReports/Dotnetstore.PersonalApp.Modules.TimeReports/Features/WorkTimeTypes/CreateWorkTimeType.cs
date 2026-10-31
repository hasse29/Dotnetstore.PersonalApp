using Dotnetstore.PersonalApp.Modules.TimeReports.Domain;
using Dotnetstore.PersonalApp.Modules.TimeReports.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.TimeReports.Features.WorkTimeTypes;

internal static class CreateWorkTimeType
{
    public sealed record Command(string Name, string? Description);

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(c => c.Name).NameRules();
            RuleFor(c => c.Description).DescriptionRules();
        }
    }

    public sealed class Handler(
        IDbContextFactory<TimeReportsDbContext> dbFactory,
        IValidator<Command> validator,
        TimeProvider clock)
    {
        public async Task<Result<WorkTimeTypeDto>> HandleAsync(Command command, CancellationToken cancellationToken)
        {
            var validation = await validator.ValidateAsync(command, cancellationToken).ConfigureAwait(false);
            if (!validation.IsValid)
            {
                return validation.ToError();
            }

            var name = command.Name.Trim();

            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            if (await db.WorkTimeTypes.AnyAsync(t => t.Name == name, cancellationToken).ConfigureAwait(false))
            {
                return Error.Conflict("work_time_type_conflict", $"A work time type named '{name}' already exists.");
            }

            var type = new WorkTimeType
            {
                Id = Guid.CreateVersion7(),
                Name = name,
                Description = WorkTimeTypeRules.NormalizeOptional(command.Description),
                CreatedAt = clock.GetUtcNow(),
            };

            db.WorkTimeTypes.Add(type);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return WorkTimeTypeDto.From(type);
        }
    }
}

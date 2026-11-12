using Dotnetstore.PersonalApp.Modules.TimeReports.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Dotnetstore.PersonalApp.Shared.Telemetry;

namespace Dotnetstore.PersonalApp.Modules.TimeReports.Features.WorkTimeTypes;

internal static class UpdateWorkTimeType
{
    public sealed record Command(Guid Id, string Name, string? Description);

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(c => c.Name).NameRules();
            RuleFor(c => c.Description).DescriptionRules();
        }
    }

    public sealed class Handler(IDbContextFactory<TimeReportsDbContext> dbFactory, IValidator<Command> validator)
    {
        public Task<Result<WorkTimeTypeDto>> HandleAsync(Command command, CancellationToken cancellationToken) =>
            AppTelemetry.TraceAsync(this, () => HandleCoreAsync(command, cancellationToken), command);

        private async Task<Result<WorkTimeTypeDto>> HandleCoreAsync(Command command, CancellationToken cancellationToken)
        {
            var validation = await validator.ValidateAsync(command, cancellationToken).ConfigureAwait(false);
            if (!validation.IsValid)
            {
                return validation.ToError();
            }

            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            var type = await db.WorkTimeTypes.FirstOrDefaultAsync(t => t.Id == command.Id, cancellationToken).ConfigureAwait(false);
            if (type is null)
            {
                return Error.NotFound("work_time_type_not_found", "Work time type not found.");
            }

            var name = command.Name.Trim();
            if (await db.WorkTimeTypes.AnyAsync(t => t.Name == name && t.Id != command.Id, cancellationToken).ConfigureAwait(false))
            {
                return Error.Conflict("work_time_type_conflict", $"A work time type named '{name}' already exists.");
            }

            type.Name = name;
            type.Description = WorkTimeTypeRules.NormalizeOptional(command.Description);

            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return WorkTimeTypeDto.From(type);
        }
    }
}

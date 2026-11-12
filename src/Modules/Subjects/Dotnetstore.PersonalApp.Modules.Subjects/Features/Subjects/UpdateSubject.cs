using Dotnetstore.PersonalApp.Modules.Subjects.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Dotnetstore.PersonalApp.Shared.Telemetry;

namespace Dotnetstore.PersonalApp.Modules.Subjects.Features.Subjects;

internal static class UpdateSubject
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

    public sealed class Handler(IDbContextFactory<SubjectsDbContext> dbFactory, IValidator<Command> validator)
    {
        public Task<Result<SubjectDto>> HandleAsync(Command command, CancellationToken cancellationToken) =>
            AppTelemetry.TraceAsync(this, () => HandleCoreAsync(command, cancellationToken), command);

        private async Task<Result<SubjectDto>> HandleCoreAsync(Command command, CancellationToken cancellationToken)
        {
            var validation = await validator.ValidateAsync(command, cancellationToken).ConfigureAwait(false);
            if (!validation.IsValid)
            {
                return validation.ToError();
            }

            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            var subject = await db.Subjects.FirstOrDefaultAsync(t => t.Id == command.Id, cancellationToken).ConfigureAwait(false);
            if (subject is null)
            {
                return Error.NotFound("subject_not_found", "Subject not found.");
            }

            var name = command.Name.Trim();
            if (await db.Subjects.AnyAsync(t => t.Name == name && t.Id != command.Id, cancellationToken).ConfigureAwait(false))
            {
                return Error.Conflict("subject_conflict", $"A subject named '{name}' already exists.");
            }

            subject.Name = name;
            subject.Description = SubjectRules.NormalizeOptional(command.Description);

            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return SubjectDto.From(subject);
        }
    }
}

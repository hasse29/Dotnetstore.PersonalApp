using Dotnetstore.PersonalApp.Modules.Subjects.Domain;
using Dotnetstore.PersonalApp.Modules.Subjects.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Subjects.Features.Subjects;

internal static class CreateSubject
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
        IDbContextFactory<SubjectsDbContext> dbFactory,
        IValidator<Command> validator,
        TimeProvider clock)
    {
        public async Task<Result<SubjectDto>> HandleAsync(Command command, CancellationToken cancellationToken)
        {
            var validation = await validator.ValidateAsync(command, cancellationToken).ConfigureAwait(false);
            if (!validation.IsValid)
            {
                return validation.ToError();
            }

            var name = command.Name.Trim();

            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            if (await db.Subjects.AnyAsync(t => t.Name == name, cancellationToken).ConfigureAwait(false))
            {
                return Error.Conflict("subject_conflict", $"A subject named '{name}' already exists.");
            }

            var subject = new Subject
            {
                Id = Guid.CreateVersion7(),
                Name = name,
                Description = SubjectRules.NormalizeOptional(command.Description),
                CreatedAt = clock.GetUtcNow(),
            };

            db.Subjects.Add(subject);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return SubjectDto.From(subject);
        }
    }
}

using Dotnetstore.PersonalApp.Modules.Students.Domain;
using Dotnetstore.PersonalApp.Modules.Students.Persistence;
using Dotnetstore.PersonalApp.Shared.Clients;
using Dotnetstore.PersonalApp.Shared.Results;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Students.Features.Students;

internal static class CreateStudent
{
    public sealed record Command(Guid ClientId, string FirstName, string LastName, string? ClassName, string? Notes);

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(c => c.FirstName).FirstNameRules();
            RuleFor(c => c.LastName).LastNameRules();
            RuleFor(c => c.ClassName).ClassNameRules();
            RuleFor(c => c.Notes).NotesRules();
        }
    }

    public sealed class Handler(
        IDbContextFactory<StudentsDbContext> dbFactory,
        IValidator<Command> validator,
        TimeProvider clock,
        IClientLookup? clients = null)
    {
        public async Task<Result<StudentDto>> HandleAsync(Command command, CancellationToken cancellationToken)
        {
            var validation = await validator.ValidateAsync(command, cancellationToken).ConfigureAwait(false);
            if (!validation.IsValid)
            {
                return validation.ToError();
            }

            // Students belong to a school in another module; without the Clients module there is nothing to check against.
            if (clients is not null && !await clients.ExistsAsync(command.ClientId, cancellationToken).ConfigureAwait(false))
            {
                return Error.NotFound("student_school_not_found", "School not found.");
            }

            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            var student = new Student
            {
                Id = Guid.CreateVersion7(),
                ClientId = command.ClientId,
                FirstName = command.FirstName.Trim(),
                LastName = command.LastName.Trim(),
                ClassName = StudentRules.NormalizeOptional(command.ClassName),
                Notes = StudentRules.NormalizeOptional(command.Notes),
                CreatedAt = clock.GetUtcNow(),
            };

            db.Students.Add(student);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return StudentDto.From(student);
        }
    }
}

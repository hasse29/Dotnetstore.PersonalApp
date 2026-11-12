using Dotnetstore.PersonalApp.Modules.Students.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Dotnetstore.PersonalApp.Shared.Telemetry;

namespace Dotnetstore.PersonalApp.Modules.Students.Features.Students;

internal static class UpdateStudent
{
    public sealed record Command(Guid Id, string FirstName, string LastName, string? ClassName, string? Notes);

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

    public sealed class Handler(IDbContextFactory<StudentsDbContext> dbFactory, IValidator<Command> validator)
    {
        public Task<Result<StudentDto>> HandleAsync(Command command, CancellationToken cancellationToken) =>
            AppTelemetry.TraceAsync(this, () => HandleCoreAsync(command, cancellationToken), command);

        private async Task<Result<StudentDto>> HandleCoreAsync(Command command, CancellationToken cancellationToken)
        {
            var validation = await validator.ValidateAsync(command, cancellationToken).ConfigureAwait(false);
            if (!validation.IsValid)
            {
                return validation.ToError();
            }

            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            var student = await db.Students.FirstOrDefaultAsync(p => p.Id == command.Id, cancellationToken).ConfigureAwait(false);
            if (student is null)
            {
                return Error.NotFound("student_not_found", "Student not found.");
            }

            student.FirstName = command.FirstName.Trim();
            student.LastName = command.LastName.Trim();
            student.ClassName = StudentRules.NormalizeOptional(command.ClassName);
            student.Notes = StudentRules.NormalizeOptional(command.Notes);

            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return StudentDto.From(student);
        }
    }
}

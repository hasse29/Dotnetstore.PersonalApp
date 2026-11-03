using Dotnetstore.PersonalApp.Modules.Students.Domain;
using Dotnetstore.PersonalApp.Modules.Students.Features.Students;
using Dotnetstore.PersonalApp.Modules.Students.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Students.Features.Notes;

internal static class AddStudentNote
{
    public sealed record Command(Guid StudentId, DateOnly Date, string Text);

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(c => c.Text).TextRules();
        }
    }

    public sealed class Handler(IDbContextFactory<StudentsDbContext> dbFactory, IValidator<Command> validator, TimeProvider clock)
    {
        public async Task<Result<StudentNoteDto>> HandleAsync(Command command, CancellationToken cancellationToken)
        {
            var validation = await validator.ValidateAsync(command, cancellationToken).ConfigureAwait(false);
            if (!validation.IsValid)
            {
                return validation.ToError();
            }

            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            if (!await db.Students.AnyAsync(s => s.Id == command.StudentId, cancellationToken).ConfigureAwait(false))
            {
                return Error.NotFound("student_not_found", "Student not found.");
            }

            var note = new StudentNote
            {
                Id = Guid.CreateVersion7(),
                StudentId = command.StudentId,
                Date = command.Date,
                Text = command.Text.Trim(),
                CreatedAt = clock.GetUtcNow(),
            };

            db.StudentNotes.Add(note);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return StudentNoteDto.From(note);
        }
    }
}

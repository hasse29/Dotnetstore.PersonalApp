using Dotnetstore.PersonalApp.Modules.Students.Features.Students;
using Dotnetstore.PersonalApp.Modules.Students.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Dotnetstore.PersonalApp.Shared.Telemetry;

namespace Dotnetstore.PersonalApp.Modules.Students.Features.Notes;

internal static class UpdateStudentNote
{
    public sealed record Command(Guid Id, DateOnly Date, string Text);

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(c => c.Text).TextRules();
        }
    }

    public sealed class Handler(IDbContextFactory<StudentsDbContext> dbFactory, IValidator<Command> validator)
    {
        public Task<Result<StudentNoteDto>> HandleAsync(Command command, CancellationToken cancellationToken) =>
            AppTelemetry.TraceAsync(this, () => HandleCoreAsync(command, cancellationToken), command);

        private async Task<Result<StudentNoteDto>> HandleCoreAsync(Command command, CancellationToken cancellationToken)
        {
            var validation = await validator.ValidateAsync(command, cancellationToken).ConfigureAwait(false);
            if (!validation.IsValid)
            {
                return validation.ToError();
            }

            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            var note = await db.StudentNotes.FirstOrDefaultAsync(n => n.Id == command.Id, cancellationToken).ConfigureAwait(false);
            if (note is null)
            {
                return Error.NotFound("student_note_not_found", "Note not found.");
            }

            note.Date = command.Date;
            note.Text = command.Text.Trim();

            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return StudentNoteDto.From(note);
        }
    }
}

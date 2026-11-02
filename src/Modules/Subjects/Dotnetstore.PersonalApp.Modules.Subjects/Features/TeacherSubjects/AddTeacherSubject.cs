using Dotnetstore.PersonalApp.Modules.Subjects.Domain;
using Dotnetstore.PersonalApp.Modules.Subjects.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Subjects.Features.TeacherSubjects;

internal static class AddTeacherSubject
{
    public sealed record Command(Guid UserId, Guid SubjectId);

    public sealed class Handler(IDbContextFactory<SubjectsDbContext> dbFactory, TimeProvider clock)
    {
        public async Task<Result<Guid>> HandleAsync(Command command, CancellationToken cancellationToken)
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            if (!await db.Subjects.AnyAsync(s => s.Id == command.SubjectId, cancellationToken).ConfigureAwait(false))
            {
                return Error.NotFound("subject_not_found", "Subject not found.");
            }

            if (await db.TeacherSubjects.AnyAsync(t => t.UserId == command.UserId && t.SubjectId == command.SubjectId, cancellationToken).ConfigureAwait(false))
            {
                return Error.Conflict("teacher_subject_conflict", "The teacher already teaches this subject.");
            }

            db.TeacherSubjects.Add(new TeacherSubject
            {
                Id = Guid.CreateVersion7(),
                UserId = command.UserId,
                SubjectId = command.SubjectId,
                CreatedAt = clock.GetUtcNow(),
            });
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return command.SubjectId;
        }
    }
}

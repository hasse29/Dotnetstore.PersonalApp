using Dotnetstore.PersonalApp.Modules.Grades.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Grades.Features.Grades;

/// <summary>Cleans up after another module deleted a teacher, a student or a subject that grades refer to.</summary>
internal static class DeleteGradesFor
{
    public sealed record Command(Guid? UserId = null, Guid? StudentId = null, Guid? SubjectId = null);

    public sealed class Handler(IDbContextFactory<GradesDbContext> dbFactory)
    {
        public async Task HandleAsync(Command command, CancellationToken cancellationToken)
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            await db.Grades
                .Where(g => (command.UserId != null && g.UserId == command.UserId)
                    || (command.StudentId != null && g.StudentId == command.StudentId)
                    || (command.SubjectId != null && g.SubjectId == command.SubjectId))
                .ExecuteDeleteAsync(cancellationToken)
                .ConfigureAwait(false);
        }
    }
}

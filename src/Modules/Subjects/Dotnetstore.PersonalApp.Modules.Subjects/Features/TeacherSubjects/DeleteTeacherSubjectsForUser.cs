using Dotnetstore.PersonalApp.Modules.Subjects.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Subjects.Features.TeacherSubjects;

internal static class DeleteTeacherSubjectsForUser
{
    public sealed record Command(Guid UserId);

    public sealed class Handler(IDbContextFactory<SubjectsDbContext> dbFactory)
    {
        public async Task<int> HandleAsync(Command command, CancellationToken cancellationToken)
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            return await db.TeacherSubjects
                .Where(t => t.UserId == command.UserId)
                .ExecuteDeleteAsync(cancellationToken)
                .ConfigureAwait(false);
        }
    }
}

using Dotnetstore.PersonalApp.Modules.Subjects.Features.Subjects;
using Dotnetstore.PersonalApp.Modules.Subjects.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Subjects.Features.TeacherSubjects;

internal static class ListTeacherSubjects
{
    public sealed record Query(Guid UserId);

    public sealed class Handler(IDbContextFactory<SubjectsDbContext> dbFactory)
    {
        /// <summary>The subjects this teacher teaches, by name.</summary>
        public async Task<IReadOnlyList<SubjectDto>> HandleAsync(Query query, CancellationToken cancellationToken)
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            return await db.TeacherSubjects.AsNoTracking()
                .Where(t => t.UserId == query.UserId)
                .Join(db.Subjects, t => t.SubjectId, s => s.Id, (_, s) => s)
                .OrderBy(s => s.Name)
                .Select(s => new SubjectDto(s.Id, s.Name, s.Description, s.CreatedAt))
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
        }
    }
}

using Dotnetstore.PersonalApp.Modules.Subjects.Persistence;
using Dotnetstore.PersonalApp.Shared.Subjects;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Subjects;

internal sealed class SubjectLookup(IDbContextFactory<SubjectsDbContext> dbFactory) : ISubjectLookup
{
    public async Task<IReadOnlyList<SubjectReference>> ListAsync(CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        return await db.Subjects.AsNoTracking()
            .OrderBy(s => s.Name)
            .Select(s => new SubjectReference(s.Id, s.Name))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<bool> ExistsAsync(Guid subjectId, CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        return await db.Subjects.AnyAsync(s => s.Id == subjectId, cancellationToken).ConfigureAwait(false);
    }
}

using Dotnetstore.PersonalApp.Modules.Students.Persistence;
using Dotnetstore.PersonalApp.Shared.Students;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Students;

internal sealed class StudentLookup(IDbContextFactory<StudentsDbContext> dbFactory) : IStudentLookup
{
    public async Task<IReadOnlyList<StudentReference>> ListForSchoolAsync(Guid clientId, CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        return await db.Students.AsNoTracking()
            .Where(s => s.ClientId == clientId)
            .OrderBy(s => s.LastName).ThenBy(s => s.FirstName)
            .Select(s => new StudentReference(s.Id, s.ClientId, s.FirstName + " " + s.LastName, s.ClassName))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<StudentReference>> ListAsync(CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        return await db.Students.AsNoTracking()
            .OrderBy(s => s.LastName).ThenBy(s => s.FirstName)
            .Select(s => new StudentReference(s.Id, s.ClientId, s.FirstName + " " + s.LastName, s.ClassName))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<bool> ExistsAsync(Guid studentId, CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        return await db.Students.AnyAsync(s => s.Id == studentId, cancellationToken).ConfigureAwait(false);
    }
}

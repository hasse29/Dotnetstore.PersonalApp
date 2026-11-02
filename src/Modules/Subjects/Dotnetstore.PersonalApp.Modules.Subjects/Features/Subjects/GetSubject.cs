using Dotnetstore.PersonalApp.Modules.Subjects.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Subjects.Features.Subjects;

internal static class GetSubject
{
    public sealed record Query(Guid Id);

    public sealed class Handler(IDbContextFactory<SubjectsDbContext> dbFactory)
    {
        public async Task<Result<SubjectDto>> HandleAsync(Query query, CancellationToken cancellationToken)
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            var subject = await db.Subjects.AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == query.Id, cancellationToken)
                .ConfigureAwait(false);

            return subject is null ? Error.NotFound("subject_not_found", "Subject not found.") : SubjectDto.From(subject);
        }
    }
}

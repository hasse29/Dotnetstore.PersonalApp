using Dotnetstore.PersonalApp.Modules.Subjects.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Subjects.Features.Subjects;

internal static class DeleteSubject
{
    public sealed record Command(Guid Id);

    /// <summary>The subject is also taken off every teacher who taught it (cascade).</summary>
    public sealed class Handler(IDbContextFactory<SubjectsDbContext> dbFactory)
    {
        public async Task<Result<Guid>> HandleAsync(Command command, CancellationToken cancellationToken)
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            var deleted = await db.Subjects
                .Where(s => s.Id == command.Id)
                .ExecuteDeleteAsync(cancellationToken)
                .ConfigureAwait(false);

            return deleted == 0 ? Error.NotFound("subject_not_found", "Subject not found.") : command.Id;
        }
    }
}

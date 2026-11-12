using Dotnetstore.PersonalApp.Modules.Subjects.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using Microsoft.EntityFrameworkCore;
using Dotnetstore.PersonalApp.Shared.Telemetry;

namespace Dotnetstore.PersonalApp.Modules.Subjects.Features.TeacherSubjects;

internal static class RemoveTeacherSubject
{
    public sealed record Command(Guid UserId, Guid SubjectId);

    public sealed class Handler(IDbContextFactory<SubjectsDbContext> dbFactory)
    {
        public Task<Result<Guid>> HandleAsync(Command command, CancellationToken cancellationToken) =>
            AppTelemetry.TraceAsync(this, () => HandleCoreAsync(command, cancellationToken), command);

        private async Task<Result<Guid>> HandleCoreAsync(Command command, CancellationToken cancellationToken)
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            var removed = await db.TeacherSubjects
                .Where(t => t.UserId == command.UserId && t.SubjectId == command.SubjectId)
                .ExecuteDeleteAsync(cancellationToken)
                .ConfigureAwait(false);

            return removed == 0 ? Error.NotFound("teacher_subject_not_found", "The teacher does not teach this subject.") : command.SubjectId;
        }
    }
}

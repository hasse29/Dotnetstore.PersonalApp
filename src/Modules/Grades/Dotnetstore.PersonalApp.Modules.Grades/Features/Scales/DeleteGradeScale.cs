using Dotnetstore.PersonalApp.Modules.Grades.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using Microsoft.EntityFrameworkCore;
using Dotnetstore.PersonalApp.Shared.Telemetry;

namespace Dotnetstore.PersonalApp.Modules.Grades.Features.Scales;

internal static class DeleteGradeScale
{
    public sealed record Command(Guid Id);

    public sealed class Handler(IDbContextFactory<GradesDbContext> dbFactory)
    {
        public Task<Result<Guid>> HandleAsync(Command command, CancellationToken cancellationToken) =>
            AppTelemetry.TraceAsync(this, () => HandleCoreAsync(command, cancellationToken), command);

        private async Task<Result<Guid>> HandleCoreAsync(Command command, CancellationToken cancellationToken)
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            if (await db.Grades.AnyAsync(g => g.ScaleItem.ScaleId == command.Id, cancellationToken).ConfigureAwait(false)
                || await db.TestResults.AnyAsync(r => r.ScaleItem != null && r.ScaleItem.ScaleId == command.Id, cancellationToken).ConfigureAwait(false))
            {
                return Error.Conflict("grade_scale_in_use", "Grades have been given on this scale, so it cannot be deleted.");
            }

            var deleted = await db.GradeScales
                .Where(s => s.Id == command.Id)
                .ExecuteDeleteAsync(cancellationToken)
                .ConfigureAwait(false);

            return deleted == 0 ? Error.NotFound("grade_scale_not_found", "Grade scale not found.") : command.Id;
        }
    }
}

using Dotnetstore.PersonalApp.Modules.Grades.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Grades.Features.Tests;

internal static class ListTestResults
{
    public sealed record Query(Guid TestId, Guid UserId);

    public sealed class Handler(IDbContextFactory<GradesDbContext> dbFactory)
    {
        /// <summary>The results of one of the teacher's tests, best score first. Student names live in another module, so the caller sorts by name if it wants to.</summary>
        public async Task<Result<IReadOnlyList<TestResultDto>>> HandleAsync(Query query, CancellationToken cancellationToken)
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            if (!await db.Tests.AnyAsync(t => t.Id == query.TestId && t.UserId == query.UserId, cancellationToken).ConfigureAwait(false))
            {
                return Error.NotFound("test_not_found", "Test not found.");
            }

            var results = await db.TestResults.AsNoTracking()
                .Include(r => r.ScaleItem)
                .Where(r => r.TestId == query.TestId)
                .OrderByDescending(r => r.Score)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            IReadOnlyList<TestResultDto> dtos = [.. results.Select(r => TestResultDto.From(r, r.ScaleItem))];

            return Result<IReadOnlyList<TestResultDto>>.Success(dtos);
        }
    }
}

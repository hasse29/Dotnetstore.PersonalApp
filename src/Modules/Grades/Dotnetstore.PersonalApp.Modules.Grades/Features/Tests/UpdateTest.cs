using Dotnetstore.PersonalApp.Modules.Grades.Features.Scales;
using Dotnetstore.PersonalApp.Modules.Grades.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Dotnetstore.PersonalApp.Shared.Telemetry;

namespace Dotnetstore.PersonalApp.Modules.Grades.Features.Tests;

internal static class UpdateTest
{
    /// <summary><paramref name="UserId"/> scopes the change to the teacher's own tests.</summary>
    public sealed record Command(Guid Id, Guid UserId, string Name, DateOnly Date, double MaxScore);

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(c => c.Name).NameRules();
            RuleFor(c => c.MaxScore).MaxScoreRules();
        }
    }

    public sealed class Handler(IDbContextFactory<GradesDbContext> dbFactory, IValidator<Command> validator)
    {
        public Task<Result<TestDto>> HandleAsync(Command command, CancellationToken cancellationToken) =>
            AppTelemetry.TraceAsync(this, () => HandleCoreAsync(command, cancellationToken), command);

        private async Task<Result<TestDto>> HandleCoreAsync(Command command, CancellationToken cancellationToken)
        {
            var validation = await validator.ValidateAsync(command, cancellationToken).ConfigureAwait(false);
            if (!validation.IsValid)
            {
                return validation.ToError();
            }

            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            var test = await db.Tests.FirstOrDefaultAsync(t => t.Id == command.Id && t.UserId == command.UserId, cancellationToken).ConfigureAwait(false);
            if (test is null)
            {
                return Error.NotFound("test_not_found", "Test not found.");
            }

            // Lowering the maximum below a score already given would leave a result that cannot be reached.
            if (await db.TestResults.AnyAsync(r => r.TestId == test.Id && r.Score > command.MaxScore, cancellationToken).ConfigureAwait(false))
            {
                return Error.Conflict("test_max_score_below_results", "A student has a higher score than this.");
            }

            test.Name = command.Name.Trim();
            test.Date = command.Date;
            test.MaxScore = command.MaxScore;

            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            var count = await db.TestResults.CountAsync(r => r.TestId == test.Id, cancellationToken).ConfigureAwait(false);

            return TestDto.From(test, count);
        }
    }
}

using Dotnetstore.PersonalApp.Modules.Grades.Domain;
using Dotnetstore.PersonalApp.Modules.Grades.Features.Scales;
using Dotnetstore.PersonalApp.Modules.Grades.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using Dotnetstore.PersonalApp.Shared.Students;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Dotnetstore.PersonalApp.Shared.Telemetry;

namespace Dotnetstore.PersonalApp.Modules.Grades.Features.Tests;

internal static class SaveTestResult
{
    /// <summary>Sets a student's result on a test: creates it, or replaces the earlier one. <paramref name="ScaleItemId"/> is the grade, which can be left out until the teacher has set it.</summary>
    public sealed record Command(Guid TestId, Guid UserId, Guid StudentId, double Score, Guid? ScaleItemId);

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(c => c.StudentId).StudentRules();
            RuleFor(c => c.Score).ScoreRules();
        }
    }

    public sealed class Handler(IDbContextFactory<GradesDbContext> dbFactory, IValidator<Command> validator, IStudentLookup students, TimeProvider clock)
    {
        public Task<Result<TestResultDto>> HandleAsync(Command command, CancellationToken cancellationToken) =>
            AppTelemetry.TraceAsync(this, () => HandleCoreAsync(command, cancellationToken), command);

        private async Task<Result<TestResultDto>> HandleCoreAsync(Command command, CancellationToken cancellationToken)
        {
            var validation = await validator.ValidateAsync(command, cancellationToken).ConfigureAwait(false);
            if (!validation.IsValid)
            {
                return validation.ToError();
            }

            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            var test = await db.Tests.AsNoTracking().FirstOrDefaultAsync(t => t.Id == command.TestId && t.UserId == command.UserId, cancellationToken).ConfigureAwait(false);
            if (test is null)
            {
                return Error.NotFound("test_not_found", "Test not found.");
            }

            if (command.Score > test.MaxScore)
            {
                return Error.Validation("test_result_score_exceeds_max", "Score is higher than the maximum score.");
            }

            if (!await students.ExistsAsync(command.StudentId, cancellationToken).ConfigureAwait(false))
            {
                return Error.NotFound("test_result_student_not_found", "Student not found.");
            }

            GradeScaleItem? item = null;
            if (command.ScaleItemId is { } itemId)
            {
                item = await db.GradeScaleItems.AsNoTracking().FirstOrDefaultAsync(i => i.Id == itemId, cancellationToken).ConfigureAwait(false);
                if (item is null)
                {
                    return Error.NotFound("grade_item_not_found", "Grade not found.");
                }
            }

            var result = await db.TestResults.FirstOrDefaultAsync(r => r.TestId == command.TestId && r.StudentId == command.StudentId, cancellationToken).ConfigureAwait(false);
            if (result is null)
            {
                result = new TestResult
                {
                    Id = Guid.CreateVersion7(),
                    TestId = command.TestId,
                    StudentId = command.StudentId,
                    CreatedAt = clock.GetUtcNow(),
                };
                db.TestResults.Add(result);
            }

            result.Score = command.Score;
            result.ScaleItemId = command.ScaleItemId;

            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return TestResultDto.From(result, item);
        }
    }
}

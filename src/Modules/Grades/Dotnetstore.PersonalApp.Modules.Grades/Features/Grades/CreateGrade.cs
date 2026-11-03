using Dotnetstore.PersonalApp.Modules.Grades.Domain;
using Dotnetstore.PersonalApp.Modules.Grades.Features.Scales;
using Dotnetstore.PersonalApp.Modules.Grades.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using Dotnetstore.PersonalApp.Shared.Students;
using Dotnetstore.PersonalApp.Shared.Subjects;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Grades.Features.Grades;

internal static class CreateGrade
{
    public sealed record Command(Guid UserId, Guid StudentId, Guid SubjectId, Guid ScaleItemId, DateOnly Date, string? Comment);

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(c => c.StudentId).StudentRules();
            RuleFor(c => c.SubjectId).SubjectRules();
            RuleFor(c => c.ScaleItemId).ScaleItemRules();
            RuleFor(c => c.Comment).CommentRules();
        }
    }

    public sealed class Handler(
        IDbContextFactory<GradesDbContext> dbFactory,
        IValidator<Command> validator,
        IStudentLookup students,
        ISubjectLookup subjects,
        TimeProvider clock)
    {
        public async Task<Result<GradeDto>> HandleAsync(Command command, CancellationToken cancellationToken)
        {
            var validation = await validator.ValidateAsync(command, cancellationToken).ConfigureAwait(false);
            if (!validation.IsValid)
            {
                return validation.ToError();
            }

            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            var step = await GradeRules.CheckAsync(db, students, subjects, command.StudentId, command.SubjectId, command.ScaleItemId, cancellationToken).ConfigureAwait(false);
            if (!step.IsSuccess)
            {
                return step.Error;
            }

            var grade = new Grade
            {
                Id = Guid.CreateVersion7(),
                UserId = command.UserId,
                StudentId = command.StudentId,
                SubjectId = command.SubjectId,
                ScaleItemId = command.ScaleItemId,
                Date = command.Date,
                Comment = GradeScaleRules.NormalizeOptional(command.Comment),
                CreatedAt = clock.GetUtcNow(),
            };

            db.Grades.Add(grade);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return GradeDto.From(grade, step.Value.Item, step.Value.ScaleName);
        }
    }
}

using Dotnetstore.PersonalApp.Modules.Grades.Features.Scales;
using Dotnetstore.PersonalApp.Modules.Grades.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using Dotnetstore.PersonalApp.Shared.Students;
using Dotnetstore.PersonalApp.Shared.Subjects;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Grades.Features.Grades;

internal static class UpdateGrade
{
    /// <summary>A grade stays with its student; the teacher can correct the subject, the grade, the date and the comment. <paramref name="UserId"/> scopes the change to the teacher's own grades.</summary>
    public sealed record Command(Guid Id, Guid UserId, Guid SubjectId, Guid ScaleItemId, DateOnly Date, string? Comment);

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(c => c.SubjectId).SubjectRules();
            RuleFor(c => c.ScaleItemId).ScaleItemRules();
            RuleFor(c => c.Comment).CommentRules();
        }
    }

    public sealed class Handler(
        IDbContextFactory<GradesDbContext> dbFactory,
        IValidator<Command> validator,
        IStudentLookup students,
        ISubjectLookup subjects)
    {
        public async Task<Result<GradeDto>> HandleAsync(Command command, CancellationToken cancellationToken)
        {
            var validation = await validator.ValidateAsync(command, cancellationToken).ConfigureAwait(false);
            if (!validation.IsValid)
            {
                return validation.ToError();
            }

            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            var grade = await db.Grades.FirstOrDefaultAsync(g => g.Id == command.Id && g.UserId == command.UserId, cancellationToken).ConfigureAwait(false);
            if (grade is null)
            {
                return Error.NotFound("grade_not_found", "Grade not found.");
            }

            var step = await GradeRules.CheckAsync(db, students, subjects, grade.StudentId, command.SubjectId, command.ScaleItemId, cancellationToken).ConfigureAwait(false);
            if (!step.IsSuccess)
            {
                return step.Error;
            }

            grade.SubjectId = command.SubjectId;
            grade.ScaleItemId = command.ScaleItemId;
            grade.Date = command.Date;
            grade.Comment = GradeScaleRules.NormalizeOptional(command.Comment);

            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return GradeDto.From(grade, step.Value.Item, step.Value.ScaleName);
        }
    }
}

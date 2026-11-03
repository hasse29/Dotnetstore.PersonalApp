using Dotnetstore.PersonalApp.Modules.Grades.Domain;
using Dotnetstore.PersonalApp.Modules.Grades.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using Dotnetstore.PersonalApp.Shared.Students;
using Dotnetstore.PersonalApp.Shared.Subjects;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Grades.Features.Grades;

/// <summary>Validation shared by create and update; the error codes (<c>grade_*</c>) are translated through <c>Error_{code}</c>.</summary>
internal static class GradeRules
{
    public const int MaxCommentLength = 500;

    /// <summary>The scale step a grade points at, together with the scale's name for the DTO.</summary>
    internal sealed record ScaleStep(GradeScaleItem Item, string ScaleName);

    public static IRuleBuilderOptions<T, Guid> StudentRules<T>(this IRuleBuilder<T, Guid> rule) =>
        rule.NotEmpty().WithErrorCode("grade_student_required").WithMessage("Choose a student.");

    public static IRuleBuilderOptions<T, Guid> SubjectRules<T>(this IRuleBuilder<T, Guid> rule) =>
        rule.NotEmpty().WithErrorCode("grade_subject_required").WithMessage("Choose a subject.");

    public static IRuleBuilderOptions<T, Guid> ScaleItemRules<T>(this IRuleBuilder<T, Guid> rule) =>
        rule.NotEmpty().WithErrorCode("grade_item_required").WithMessage("Choose a grade.");

    public static IRuleBuilderOptions<T, string?> CommentRules<T>(this IRuleBuilder<T, string?> rule) =>
        rule.MaximumLength(MaxCommentLength).WithErrorCode("grade_comment_too_long").WithMessage("Comment is too long.");

    /// <summary>The student and subject live in other modules and the scale step in this one; each must exist.</summary>
    public static async Task<Result<ScaleStep>> CheckAsync(
        GradesDbContext db,
        IStudentLookup students,
        ISubjectLookup subjects,
        Guid studentId,
        Guid subjectId,
        Guid scaleItemId,
        CancellationToken cancellationToken)
    {
        if (!await students.ExistsAsync(studentId, cancellationToken).ConfigureAwait(false))
        {
            return Error.NotFound("grade_student_not_found", "Student not found.");
        }

        if (!await subjects.ExistsAsync(subjectId, cancellationToken).ConfigureAwait(false))
        {
            return Error.NotFound("grade_subject_not_found", "Subject not found.");
        }

        var item = await db.GradeScaleItems.AsNoTracking().FirstOrDefaultAsync(i => i.Id == scaleItemId, cancellationToken).ConfigureAwait(false);
        if (item is null)
        {
            return Error.NotFound("grade_item_not_found", "Grade not found.");
        }

        var scaleName = await db.GradeScales.Where(s => s.Id == item.ScaleId).Select(s => s.Name).FirstAsync(cancellationToken).ConfigureAwait(false);

        return new ScaleStep(item, scaleName);
    }
}

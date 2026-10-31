using Dotnetstore.PersonalApp.Modules.Cvs.Persistence;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Cvs.Features.Cvs;

/// <summary>Validation rules and the ownership check shared by the operations on a CV's sections.</summary>
internal static class CvSectionRules
{
    public const int MaxNameLength = 200;
    public const int MaxDescriptionLength = 2000;
    public const int MaxLanguageLength = 100;
    public const int MinCourseYear = 1950;

    public static IRuleBuilderOptions<T, string> RequiredText<T>(this IRuleBuilder<T, string> rule, string requiredCode, string tooLongCode, int maxLength) =>
        rule.NotEmpty().WithErrorCode(requiredCode).WithMessage("A required field is missing.")
            .MaximumLength(maxLength).WithErrorCode(tooLongCode).WithMessage("A field is too long.");

    public static IRuleBuilderOptions<T, string?> OptionalText<T>(this IRuleBuilder<T, string?> rule, string tooLongCode, int maxLength) =>
        rule.MaximumLength(maxLength).WithErrorCode(tooLongCode).WithMessage("A field is too long.");

    public static IRuleBuilderOptions<T, Guid> CvIdRules<T>(this IRuleBuilder<T, Guid> rule) =>
        rule.NotEmpty().WithErrorCode("cv_not_found").WithMessage("CV not found.");

    public static IRuleBuilderOptions<T, DateOnly> StartDateRules<T>(this IRuleBuilder<T, DateOnly> rule) =>
        rule.NotEqual(default(DateOnly)).WithErrorCode("cv_start_date_required").WithMessage("Start date is required.");

    public static IRuleBuilderOptions<T, DateOnly?> EndDateRules<T>(this IRuleBuilder<T, DateOnly?> rule, Func<T, DateOnly> startDate) =>
        rule.Must((command, end) => end is null || end >= startDate(command))
            .WithErrorCode("cv_end_before_start").WithMessage("End date cannot be before the start date.");

    /// <summary>True when the CV exists and belongs to the user; someone else's CV is treated as not found.</summary>
    public static Task<bool> OwnsCvAsync(this CvsDbContext db, Guid cvId, Guid userId, CancellationToken cancellationToken) =>
        db.Cvs.AnyAsync(c => c.Id == cvId && c.UserId == userId, cancellationToken);
}

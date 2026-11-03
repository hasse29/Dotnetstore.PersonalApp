using FluentValidation;

namespace Dotnetstore.PersonalApp.Modules.Grades.Features.Tests;

/// <summary>Validation shared by the test handlers; the error codes (<c>test_*</c>) are translated through <c>Error_{code}</c>.</summary>
internal static class TestRules
{
    public const int MaxNameLength = 100;
    public const double MaxScoreLimit = 100_000;

    public static IRuleBuilderOptions<T, string> NameRules<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty().WithErrorCode("test_name_required").WithMessage("Name is required.")
            .MaximumLength(MaxNameLength).WithErrorCode("test_name_too_long").WithMessage("Name is too long.");

    public static IRuleBuilderOptions<T, double> MaxScoreRules<T>(this IRuleBuilder<T, double> rule) =>
        rule.Must(score => double.IsFinite(score) && score > 0 && score <= MaxScoreLimit)
            .WithErrorCode("test_max_score_invalid").WithMessage("Maximum score must be more than 0.");

    public static IRuleBuilderOptions<T, Guid> StudentRules<T>(this IRuleBuilder<T, Guid> rule) =>
        rule.NotEmpty().WithErrorCode("test_result_student_required").WithMessage("Choose a student.");

    public static IRuleBuilderOptions<T, double> ScoreRules<T>(this IRuleBuilder<T, double> rule) =>
        rule.Must(score => double.IsFinite(score) && score >= 0)
            .WithErrorCode("test_result_score_invalid").WithMessage("Score cannot be negative.");
}

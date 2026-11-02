using Dotnetstore.PersonalApp.Shared.Results;
using FluentValidation;
using FluentValidation.Results;

namespace Dotnetstore.PersonalApp.Modules.Students.Features.Students;

/// <summary>Shared validation rules; the error codes (<c>student_*</c>) are translated through <c>Error_{code}</c>.</summary>
internal static class StudentRules
{
    public const int MaxNameLength = 100;
    public const int MaxClassNameLength = 100;
    public const int MaxNotesLength = 2000;

    /// <summary>Optional text fields: trimmed, and empty becomes null.</summary>
    public static string? NormalizeOptional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    public static Error ToError(this ValidationResult result)
    {
        var failure = result.Errors[0];
        return Error.Validation(failure.ErrorCode, failure.ErrorMessage);
    }

    public static IRuleBuilderOptions<T, string> FirstNameRules<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty().WithErrorCode("student_first_name_required").WithMessage("First name is required.")
            .MaximumLength(MaxNameLength).WithErrorCode("student_first_name_too_long").WithMessage("First name is too long.");

    public static IRuleBuilderOptions<T, string> LastNameRules<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty().WithErrorCode("student_last_name_required").WithMessage("Last name is required.")
            .MaximumLength(MaxNameLength).WithErrorCode("student_last_name_too_long").WithMessage("Last name is too long.");

    public static IRuleBuilderOptions<T, string?> ClassNameRules<T>(this IRuleBuilder<T, string?> rule) =>
        rule.MaximumLength(MaxClassNameLength).WithErrorCode("student_class_name_too_long").WithMessage("Class is too long.");

    public static IRuleBuilderOptions<T, string?> NotesRules<T>(this IRuleBuilder<T, string?> rule) =>
        rule.MaximumLength(MaxNotesLength).WithErrorCode("student_notes_too_long").WithMessage("Notes are too long.");
}

using FluentValidation;

namespace Dotnetstore.PersonalApp.Modules.Students.Features.Notes;

/// <summary>Validation shared by add and update; error codes (<c>student_note_*</c>) are translated through <c>Error_{code}</c>.</summary>
internal static class StudentNoteRules
{
    public const int MaxTextLength = 4000;

    public static IRuleBuilderOptions<T, string> TextRules<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty().WithErrorCode("student_note_text_required").WithMessage("Write something in the note.")
            .MaximumLength(MaxTextLength).WithErrorCode("student_note_text_too_long").WithMessage("The note is too long.");
}

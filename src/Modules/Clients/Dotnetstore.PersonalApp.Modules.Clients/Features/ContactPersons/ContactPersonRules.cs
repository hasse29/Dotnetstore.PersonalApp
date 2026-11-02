using FluentValidation;

namespace Dotnetstore.PersonalApp.Modules.Clients.Features.ContactPersons;

/// <summary>Shared validation rules; the error codes (<c>contact_person_*</c>) are translated through <c>Error_{code}</c>.</summary>
internal static class ContactPersonRules
{
    public const int MaxNameLength = 100;
    public const int MaxJobTitleLength = 100;
    public const int MaxNotesLength = 2000;

    public static IRuleBuilderOptions<T, string> FirstNameRules<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty().WithErrorCode("contact_person_first_name_required").WithMessage("First name is required.")
            .MaximumLength(MaxNameLength).WithErrorCode("contact_person_first_name_too_long").WithMessage("First name is too long.");

    public static IRuleBuilderOptions<T, string> LastNameRules<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty().WithErrorCode("contact_person_last_name_required").WithMessage("Last name is required.")
            .MaximumLength(MaxNameLength).WithErrorCode("contact_person_last_name_too_long").WithMessage("Last name is too long.");

    public static IRuleBuilderOptions<T, string?> JobTitleRules<T>(this IRuleBuilder<T, string?> rule) =>
        rule.MaximumLength(MaxJobTitleLength).WithErrorCode("contact_person_job_title_too_long").WithMessage("Job title is too long.");

    public static IRuleBuilderOptions<T, string?> NotesRules<T>(this IRuleBuilder<T, string?> rule) =>
        rule.MaximumLength(MaxNotesLength).WithErrorCode("contact_person_notes_too_long").WithMessage("Notes are too long.");
}

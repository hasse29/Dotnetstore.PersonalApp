using Dotnetstore.PersonalApp.Modules.Schedule.Persistence;
using Dotnetstore.PersonalApp.Shared.Clients;
using Dotnetstore.PersonalApp.Shared.Results;
using Dotnetstore.PersonalApp.Shared.Students;
using Dotnetstore.PersonalApp.Shared.Subjects;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Schedule.Features.Entries;

/// <summary>
/// Shared validation rules. Each rule carries an error code (e.g. <c>schedule_end_before_start</c>) that the UI translates via
/// the <c>Error_{code}</c> resource; the English message is only a fallback.
/// </summary>
internal static class ScheduleRules
{
    public const int MaxRoomLength = 100;
    public const int MaxNotesLength = 500;
    public const int MaxStudents = 200;

    public static IRuleBuilderOptions<T, DayOfWeek> DayRules<T>(this IRuleBuilder<T, DayOfWeek> rule) =>
        rule.IsInEnum().WithErrorCode("schedule_day_invalid").WithMessage("Choose a day of the week.");

    public static IRuleBuilderOptions<T, Guid> SubjectRules<T>(this IRuleBuilder<T, Guid> rule) =>
        rule.NotEmpty().WithErrorCode("schedule_subject_required").WithMessage("A subject is required.");

    public static IRuleBuilderOptions<T, Guid> SchoolRules<T>(this IRuleBuilder<T, Guid> rule) =>
        rule.NotEmpty().WithErrorCode("schedule_school_required").WithMessage("A school is required.");

    public static IRuleBuilderOptions<T, string?> RoomRules<T>(this IRuleBuilder<T, string?> rule) =>
        rule.MaximumLength(MaxRoomLength).WithErrorCode("schedule_room_too_long").WithMessage("Room is too long.");

    public static IRuleBuilderOptions<T, string?> NotesRules<T>(this IRuleBuilder<T, string?> rule) =>
        rule.MaximumLength(MaxNotesLength).WithErrorCode("schedule_notes_too_long").WithMessage("Notes are too long.");

    /// <summary>A lesson cannot run past midnight, so it is simply an ordered pair of times on one day.</summary>
    public static IRuleBuilderOptions<T, TimeOnly> EndRules<T>(this IRuleBuilder<T, TimeOnly> rule, Func<T, TimeOnly> start) =>
        rule.Must((command, end) => end > start(command)).WithErrorCode("schedule_end_before_start").WithMessage("The lesson must end after it starts.");

    public static IRuleBuilderOptions<T, IReadOnlyList<Guid>> StudentRules<T>(this IRuleBuilder<T, IReadOnlyList<Guid>> rule) =>
        rule.Must(ids => ids.Count <= MaxStudents).WithErrorCode("schedule_too_many_students").WithMessage("Too many students.");

    /// <summary>Optional text fields: trimmed, and empty becomes null.</summary>
    public static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    public static Error ToError(this ValidationResult result)
    {
        var failure = result.Errors[0];
        return Error.Validation(failure.ErrorCode, failure.ErrorMessage);
    }

    /// <summary>
    /// The checks that need data from elsewhere: the subject and school must exist, the students must belong to that school, and the teacher cannot be in two lessons at once.
    /// Returns null when the entry is fine. <paramref name="ignoreEntryId"/> is the entry being edited.
    /// </summary>
    public static async Task<Error?> CheckAsync(
        ScheduleDbContext db,
        ISubjectLookup subjects,
        IClientLookup clients,
        IStudentLookup students,
        Guid userId,
        DayOfWeek day,
        TimeOnly start,
        TimeOnly end,
        Guid subjectId,
        Guid clientId,
        IReadOnlyList<Guid> studentIds,
        Guid? ignoreEntryId,
        CancellationToken cancellationToken)
    {
        if (!await subjects.ExistsAsync(subjectId, cancellationToken).ConfigureAwait(false))
        {
            return Error.NotFound("schedule_subject_not_found", "Subject not found.");
        }

        if (!await clients.ExistsAsync(clientId, cancellationToken).ConfigureAwait(false))
        {
            return Error.NotFound("schedule_school_not_found", "School not found.");
        }

        if (studentIds.Count > 0)
        {
            var ofSchool = (await students.ListForSchoolAsync(clientId, cancellationToken).ConfigureAwait(false)).Select(s => s.Id).ToHashSet();
            if (!studentIds.All(ofSchool.Contains))
            {
                return Error.NotFound("schedule_student_not_found", "A student was not found at that school.");
            }
        }

        // A teacher has a handful of lessons a day, so comparing the times in memory keeps the query trivial.
        var sameDay = await db.ScheduleEntries.AsNoTracking()
            .Where(e => e.UserId == userId && e.Day == day && e.Id != ignoreEntryId)
            .Select(e => new { e.StartTime, e.EndTime })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return sameDay.Any(e => e.StartTime < end && start < e.EndTime)
            ? Error.Conflict("schedule_overlap", "The teacher already has a lesson at that time.")
            : null;
    }
}

using Dotnetstore.PersonalApp.Shared.Localization;

namespace Dotnetstore.PersonalApp.Modules.Schedule.Features.Entries;

/// <summary>A weekday as the user picks it, named in the current language.</summary>
internal sealed record DayOption(DayOfWeek Day, string Name)
{
    /// <summary>The working week first: Monday to Sunday.</summary>
    public static IReadOnlyList<DayOption> All(ILocalizationService localization)
    {
        var format = localization.Culture.DateTimeFormat;
        return
        [
            .. new[]
            {
                DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday,
                DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday,
            }.Select(day => new DayOption(day, Capitalize(format.GetDayName(day), localization))),
        ];
    }

    private static string Capitalize(string name, ILocalizationService localization) =>
        name.Length == 0 ? name : char.ToUpper(name[0], localization.Culture) + name[1..];
}

/// <summary>A lesson with the subject and school names already looked up, ready to show in the table.</summary>
internal sealed record ScheduleRow(ScheduleEntryDto Entry, string DayName, string SubjectName, string SchoolName)
{
    public string TimeRange => $"{Entry.StartTime:HH:mm}–{Entry.EndTime:HH:mm}";

    public string? Room => Entry.Room;
}

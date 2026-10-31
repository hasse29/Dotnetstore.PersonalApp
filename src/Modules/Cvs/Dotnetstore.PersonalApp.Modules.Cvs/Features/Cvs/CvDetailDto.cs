using System.Globalization;
using Dotnetstore.PersonalApp.Modules.Cvs.Domain;
using Dotnetstore.PersonalApp.Shared.Contacts;
using Dotnetstore.PersonalApp.Shared.Users;

namespace Dotnetstore.PersonalApp.Modules.Cvs.Features.Cvs;

/// <param name="User">Null when the user no longer exists.</param>
/// <param name="Photo">The photo's bytes, or null when the CV has none.</param>
internal sealed record CvDetailDto(
    CvDto Cv,
    UserReference? User,
    byte[]? Photo,
    IReadOnlyList<string> DriverLicence,
    IReadOnlyList<WorkExperienceDto> WorkExperiences,
    IReadOnlyList<EducationDto> Educations,
    IReadOnlyList<CourseDto> Courses,
    IReadOnlyList<LanguageSkillDto> Languages,
    IReadOnlyList<ContactEntry> AvailableContacts,
    IReadOnlyList<Guid> SelectedContactIds)
{
    /// <summary>The contact entries chosen for this CV, in display order; ones deleted since are left out.</summary>
    public IEnumerable<ContactEntry> SelectedContacts => AvailableContacts.Where(c => SelectedContactIds.Contains(c.Id));
}

internal sealed record WorkExperienceDto(
    Guid Id, string Employer, string Role, DateOnly StartDate, DateOnly? EndDate, string? Description, IReadOnlyList<string> Technologies)
{
    public static WorkExperienceDto From(WorkExperience w) => new(w.Id, w.Employer, w.Role, w.StartDate, w.EndDate, w.Description, TechnologyTags.Parse(w.Technologies));
}

internal sealed record EducationDto(Guid Id, string School, string Programme, DateOnly StartDate, DateOnly? EndDate, string? Description)
{
    public static EducationDto From(Education e) => new(e.Id, e.School, e.Programme, e.StartDate, e.EndDate, e.Description);
}

internal sealed record CourseDto(Guid Id, string Name, string? Provider, int? CompletedYear)
{
    public static CourseDto From(Course c) => new(c.Id, c.Name, c.Provider, c.CompletedYear);
}

internal sealed record LanguageSkillDto(Guid Id, string Language, LanguageLevel Level)
{
    public static LanguageSkillDto From(LanguageSkill l) => new(l.Id, l.Language, l.Level);
}

internal static class PeriodFormat
{
    /// <summary>"2020-01 – 2023-06", or "2020-01 – {present}" while ongoing.</summary>
    public static string Format(DateOnly start, DateOnly? end, string present) =>
        string.Create(CultureInfo.InvariantCulture, $"{start:yyyy-MM} – ") + (end is { } e ? e.ToString("yyyy-MM", CultureInfo.InvariantCulture) : present);
}

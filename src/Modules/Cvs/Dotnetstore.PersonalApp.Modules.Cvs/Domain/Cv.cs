namespace Dotnetstore.PersonalApp.Modules.Cvs.Domain;

/// <summary>
/// One CV of one user, written for a particular kind of job. <see cref="UserId"/> refers to an object owned by the Users
/// module; it is not a foreign key, and a user's CVs are removed through the <c>UserDeleted</c> integration event.
/// The CV's sections (work experience, education, courses, languages) are rows that point back at it and go with it.
/// </summary>
internal sealed class Cv
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    /// <summary>Unique per user, ignoring case, so the user can tell the CVs apart.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>The job or role this CV is aimed at.</summary>
    public string? TargetJob { get; set; }

    public string? Summary { get; set; }

    /// <summary>The https address of the LinkedIn profile, or null.</summary>
    public string? LinkedIn { get; set; }

    /// <summary>The photo's bytes (PNG or JPEG); each CV can use its own picture.</summary>
    public byte[]? Photo { get; set; }

    /// <summary>Driver licence categories as a comma-separated list in canonical order, e.g. <c>A,B,BE</c>; empty for none.</summary>
    public string DriverLicence { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}

namespace Dotnetstore.PersonalApp.Modules.Cvs.Domain;

internal sealed class WorkExperience
{
    public Guid Id { get; set; }

    public Guid CvId { get; set; }

    public string Employer { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;

    public DateOnly StartDate { get; set; }

    /// <summary>Null while the job is ongoing.</summary>
    public DateOnly? EndDate { get; set; }

    public string? Description { get; set; }

    /// <summary>Technologies used in this job as a comma-separated list of tags, e.g. <c>C#,.NET,SQL</c>; empty for none.</summary>
    public string Technologies { get; set; } = string.Empty;
}

internal sealed class Education
{
    public Guid Id { get; set; }

    public Guid CvId { get; set; }

    public string School { get; set; } = string.Empty;

    public string Programme { get; set; } = string.Empty;

    public DateOnly StartDate { get; set; }

    /// <summary>Null while the education is ongoing.</summary>
    public DateOnly? EndDate { get; set; }

    public string? Description { get; set; }
}

/// <summary>A short course or certificate.</summary>
internal sealed class Course
{
    public Guid Id { get; set; }

    public Guid CvId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Provider { get; set; }

    public int? CompletedYear { get; set; }
}

internal sealed class LanguageSkill
{
    public Guid Id { get; set; }

    public Guid CvId { get; set; }

    public string Language { get; set; } = string.Empty;

    public LanguageLevel Level { get; set; }
}

internal enum LanguageLevel
{
    Basic,
    Good,
    Fluent,
    Native,
}

/// <summary>
/// Says that an address, phone number or email address of the CV's user is shown on the CV. <see cref="ContactId"/> belongs to
/// another module, so it is not a foreign key; an entry that has since been deleted there is simply no longer shown.
/// </summary>
internal sealed class CvContactSelection
{
    public Guid Id { get; set; }

    public Guid CvId { get; set; }

    public Guid ContactId { get; set; }
}

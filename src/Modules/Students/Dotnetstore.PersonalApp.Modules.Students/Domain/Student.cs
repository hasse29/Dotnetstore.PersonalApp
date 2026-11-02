namespace Dotnetstore.PersonalApp.Modules.Students.Domain;

/// <summary>A student at a school (a client of the teacher). Its addresses, phone numbers and email addresses live in other modules, keyed by <see cref="Id"/>; <see cref="ClientId"/> is the school's id in the Clients module, so there is no foreign key.</summary>
internal sealed class Student
{
    public Guid Id { get; set; }

    public Guid ClientId { get; set; }

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string? ClassName { get; set; }

    public string? Notes { get; set; }

    public byte[]? Photo { get; set; }

    /// <summary>A small square copy of <see cref="Photo"/> for lists.</summary>
    public byte[]? PhotoThumbnail { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}

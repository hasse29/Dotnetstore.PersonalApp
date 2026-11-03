namespace Dotnetstore.PersonalApp.Modules.Students.Domain;

/// <summary>One entry in a student's running notes: what the teacher observed or did on <see cref="Date"/>.</summary>
internal sealed class StudentNote
{
    public Guid Id { get; set; }

    public Guid StudentId { get; set; }

    /// <summary>The day the note is about, chosen by the teacher; <see cref="CreatedAt"/> only breaks ties within a day.</summary>
    public DateOnly Date { get; set; }

    public string Text { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }
}

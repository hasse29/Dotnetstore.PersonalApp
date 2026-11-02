namespace Dotnetstore.PersonalApp.Shared.Subjects;

/// <summary>A subject as other modules see it: just enough to pick one and show its name.</summary>
public sealed record SubjectReference(Guid Id, string Name);

/// <summary>Implemented by the Subjects module so that other modules can refer to subjects without referencing it.</summary>
public interface ISubjectLookup
{
    /// <summary>All subjects ordered by name.</summary>
    Task<IReadOnlyList<SubjectReference>> ListAsync(CancellationToken cancellationToken);

    /// <summary>Whether a subject with this id exists.</summary>
    Task<bool> ExistsAsync(Guid subjectId, CancellationToken cancellationToken);
}

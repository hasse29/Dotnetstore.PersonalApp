namespace Dotnetstore.PersonalApp.Shared.Students;

/// <summary>A student as other modules see it: just enough to pick one and show their name.</summary>
public sealed record StudentReference(Guid Id, Guid ClientId, string Name, string? ClassName);

/// <summary>Implemented by the Students module so that other modules can refer to students without referencing it.</summary>
public interface IStudentLookup
{
    /// <summary>The students of one school (client), ordered by name.</summary>
    Task<IReadOnlyList<StudentReference>> ListForSchoolAsync(Guid clientId, CancellationToken cancellationToken);

    /// <summary>The students of every school, ordered by name.</summary>
    Task<IReadOnlyList<StudentReference>> ListAsync(CancellationToken cancellationToken);

    /// <summary>Whether a student with this id exists. The default reads the whole list; Students overrides it with a single query.</summary>
    async Task<bool> ExistsAsync(Guid studentId, CancellationToken cancellationToken) =>
        (await ListAsync(cancellationToken).ConfigureAwait(false)).Any(s => s.Id == studentId);
}

namespace Dotnetstore.PersonalApp.Shared.Students;

/// <summary>A student as other modules see it: just enough to pick one and show their name.</summary>
public sealed record StudentReference(Guid Id, Guid ClientId, string Name, string? ClassName);

/// <summary>Implemented by the Students module so that other modules can refer to students without referencing it.</summary>
public interface IStudentLookup
{
    /// <summary>The students of one school (client), ordered by name.</summary>
    Task<IReadOnlyList<StudentReference>> ListForSchoolAsync(Guid clientId, CancellationToken cancellationToken);
}

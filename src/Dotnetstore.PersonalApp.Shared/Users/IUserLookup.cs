namespace Dotnetstore.PersonalApp.Shared.Users;

/// <summary>A user as other modules see it: the personal details, never the credentials.</summary>
public sealed record UserReference(Guid Id, string FirstName, string LastName, string Username, DateOnly DateOfBirth)
{
    public string FullName => $"{FirstName} {LastName}";
}

/// <summary>Implemented by the Users module so that other modules can show a user's details without referencing it.</summary>
public interface IUserLookup
{
    /// <returns>The user, or null when no user has this id.</returns>
    Task<UserReference?> GetAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Every user ordered by name, for pickers in other modules. The default is empty; Users overrides it.</summary>
    Task<IReadOnlyList<UserReference>> ListAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<UserReference>>([]);
}


/// <summary>Implemented by the Users module so that the shell can show the signed-in user's picture without referencing it.</summary>
public interface IUserPhotoLookup
{
    /// <returns>The small thumbnail, or null when the user has no photo.</returns>
    Task<byte[]?> GetThumbnailAsync(Guid userId, CancellationToken cancellationToken);
}

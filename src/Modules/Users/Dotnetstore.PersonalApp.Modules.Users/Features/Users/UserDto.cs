using Dotnetstore.PersonalApp.Modules.Users.Domain;
using Dotnetstore.PersonalApp.Shared.Photos;

namespace Dotnetstore.PersonalApp.Modules.Users.Features.Users;

/// <param name="Thumbnail">The small picture for lists; the full photo is read on its own.</param>
internal sealed record UserDto(
    Guid Id,
    string FirstName,
    string LastName,
    string Username,
    DateOnly DateOfBirth,
    DateTimeOffset CreatedAt,
    byte[]? Thumbnail = null)
{
    public string FullName => $"{FirstName} {LastName}";

    public string Initials => PhotoInitials.From(FullName);

    public static UserDto From(User user) =>
        new(user.Id, user.FirstName, user.LastName, user.Username, user.DateOfBirth, user.CreatedAt, user.PhotoThumbnail);
}

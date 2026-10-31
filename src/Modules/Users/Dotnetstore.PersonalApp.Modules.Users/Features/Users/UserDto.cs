using Dotnetstore.PersonalApp.Modules.Users.Domain;

namespace Dotnetstore.PersonalApp.Modules.Users.Features.Users;

internal sealed record UserDto(
    Guid Id,
    string FirstName,
    string LastName,
    string Username,
    DateOnly DateOfBirth,
    DateTimeOffset CreatedAt)
{
    public string FullName => $"{FirstName} {LastName}";

    public static UserDto From(User user) =>
        new(user.Id, user.FirstName, user.LastName, user.Username, user.DateOfBirth, user.CreatedAt);
}

using Dotnetstore.PersonalApp.Modules.Users.Domain;
using Dotnetstore.PersonalApp.Modules.Users.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Users.Features.Users;

internal static class UpdateUser
{
    /// <param name="Id">The user to update.</param>
    /// <param name="FirstName">New first name.</param>
    /// <param name="LastName">New last name.</param>
    /// <param name="Username">New username (email address).</param>
    /// <param name="NewPassword">Null or empty keeps the current password.</param>
    /// <param name="DateOfBirth">New date of birth.</param>
    public sealed record Command(Guid Id, string FirstName, string LastName, string Username, string? NewPassword, DateOnly DateOfBirth);

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator(TimeProvider clock)
        {
            RuleFor(c => c.FirstName).NameRules("first_name");
            RuleFor(c => c.LastName).NameRules("last_name");
            RuleFor(c => c.Username).UsernameRules();
            RuleFor(c => c.NewPassword!).PasswordRules().When(c => !string.IsNullOrEmpty(c.NewPassword));
            RuleFor(c => c.DateOfBirth).DateOfBirthRules(clock);
        }
    }

    public sealed class Handler(
        IDbContextFactory<UsersDbContext> dbFactory,
        IValidator<Command> validator,
        IPasswordHasher<User> hasher)
    {
        public async Task<Result<UserDto>> HandleAsync(Command command, CancellationToken cancellationToken)
        {
            var validation = await validator.ValidateAsync(command, cancellationToken).ConfigureAwait(false);
            if (!validation.IsValid)
            {
                return validation.ToError();
            }

            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            var user = await db.Users.FirstOrDefaultAsync(u => u.Id == command.Id, cancellationToken).ConfigureAwait(false);
            if (user is null)
            {
                return Error.NotFound("user_not_found", "User not found.");
            }

            var username = UserRules.NormalizeUsername(command.Username);
            if (await db.Users.AnyAsync(u => u.Username == username && u.Id != command.Id, cancellationToken).ConfigureAwait(false))
            {
                return Error.Conflict("user_conflict", $"A user with username '{username}' already exists.");
            }

            user.FirstName = command.FirstName.Trim();
            user.LastName = command.LastName.Trim();
            user.Username = username;
            user.DateOfBirth = command.DateOfBirth;
            if (!string.IsNullOrEmpty(command.NewPassword))
            {
                user.PasswordHash = hasher.HashPassword(user, command.NewPassword);
            }

            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return UserDto.From(user);
        }
    }
}

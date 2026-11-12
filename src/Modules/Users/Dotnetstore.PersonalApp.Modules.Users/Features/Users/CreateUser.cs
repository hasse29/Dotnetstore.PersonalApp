using Dotnetstore.PersonalApp.Modules.Users.Domain;
using Dotnetstore.PersonalApp.Modules.Users.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Dotnetstore.PersonalApp.Shared.Telemetry;

namespace Dotnetstore.PersonalApp.Modules.Users.Features.Users;

internal static class CreateUser
{
    public sealed record Command(string FirstName, string LastName, string Username, string Password, DateOnly DateOfBirth);

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator(TimeProvider clock)
        {
            RuleFor(c => c.FirstName).NameRules("first_name");
            RuleFor(c => c.LastName).NameRules("last_name");
            RuleFor(c => c.Username).UsernameRules();
            RuleFor(c => c.Password).PasswordRules();
            RuleFor(c => c.DateOfBirth).DateOfBirthRules(clock);
        }
    }

    public sealed class Handler(
        IDbContextFactory<UsersDbContext> dbFactory,
        IValidator<Command> validator,
        IPasswordHasher<User> hasher,
        TimeProvider clock)
    {
        public Task<Result<UserDto>> HandleAsync(Command command, CancellationToken cancellationToken) =>
            AppTelemetry.TraceAsync(this, () => HandleCoreAsync(command, cancellationToken), command);

        private async Task<Result<UserDto>> HandleCoreAsync(Command command, CancellationToken cancellationToken)
        {
            var validation = await validator.ValidateAsync(command, cancellationToken).ConfigureAwait(false);
            if (!validation.IsValid)
            {
                return validation.ToError();
            }

            var username = UserRules.NormalizeUsername(command.Username);

            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            if (await db.Users.AnyAsync(u => u.Username == username, cancellationToken).ConfigureAwait(false))
            {
                return Error.Conflict("user_conflict", $"A user with username '{username}' already exists.");
            }

            var user = new User
            {
                Id = Guid.CreateVersion7(),
                FirstName = command.FirstName.Trim(),
                LastName = command.LastName.Trim(),
                Username = username,
                DateOfBirth = command.DateOfBirth,
                CreatedAt = clock.GetUtcNow(),
            };
            user.PasswordHash = hasher.HashPassword(user, command.Password);

            db.Users.Add(user);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return UserDto.From(user);
        }
    }
}

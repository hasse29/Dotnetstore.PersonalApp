using Dotnetstore.PersonalApp.Modules.Users.Domain;
using Dotnetstore.PersonalApp.Modules.Users.Persistence;
using Dotnetstore.PersonalApp.Modules.Users.Features.Users;
using Dotnetstore.PersonalApp.Shared.Auth;
using Dotnetstore.PersonalApp.Shared.Results;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Users.Features.Authentication;

internal static class Login
{
    public sealed record Command(string Username, string Password);

    public sealed class Handler(IDbContextFactory<UsersDbContext> dbFactory, IPasswordHasher<User> hasher)
    {
        // Verified against when the username is unknown, so response time does not reveal which usernames exist.
        private readonly Lazy<string> _dummyHash = new(() => hasher.HashPassword(new User(), "dummy-password-for-timing"));

        public async Task<Result<AuthenticatedUser>> HandleAsync(Command command, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(command.Username) || string.IsNullOrEmpty(command.Password))
            {
                return Error.Validation("credentials_required", "Enter your username and password.");
            }

            var username = UserRules.NormalizeUsername(command.Username);

            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            var user = await db.Users.FirstOrDefaultAsync(u => u.Username == username, cancellationToken).ConfigureAwait(false);
            if (user is null)
            {
                hasher.VerifyHashedPassword(new User(), _dummyHash.Value, command.Password);
                return Error.InvalidCredentials();
            }

            var verification = hasher.VerifyHashedPassword(user, user.PasswordHash, command.Password);
            if (verification == PasswordVerificationResult.Failed)
            {
                return Error.InvalidCredentials();
            }

            if (verification == PasswordVerificationResult.SuccessRehashNeeded)
            {
                user.PasswordHash = hasher.HashPassword(user, command.Password);
                await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }

            return new AuthenticatedUser(user.Id, user.Username, $"{user.FirstName} {user.LastName}");
        }
    }
}

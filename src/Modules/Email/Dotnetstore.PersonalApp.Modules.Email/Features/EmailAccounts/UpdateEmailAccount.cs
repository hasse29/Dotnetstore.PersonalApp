using Dotnetstore.PersonalApp.Modules.Email.Domain;
using Dotnetstore.PersonalApp.Modules.Email.Persistence;
using Dotnetstore.PersonalApp.Modules.Email.Security;
using Dotnetstore.PersonalApp.Shared.Results;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Email.Features.EmailAccounts;

/// <summary>
/// Changes one of the user's own accounts or a system account; someone else's account is reported as not found.
/// A null <c>Password</c> keeps the stored one. Whether an account is personal or system cannot be changed.
/// </summary>
internal static class UpdateEmailAccount
{
    public sealed record Command(
        Guid Id,
        Guid UserId,
        string Name,
        MailProtocol Protocol,
        string Host,
        int Port,
        ConnectionSecurity Security,
        string EmailAddress,
        string? Username,
        string? Password);

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(c => c.UserId).UserIdRules();
            RuleFor(c => c.Name).NameRules();
            RuleFor(c => c.Protocol).ProtocolRules();
            RuleFor(c => c.Host).HostRules();
            RuleFor(c => c.Port).PortRules();
            RuleFor(c => c.Security).SecurityRules();
            RuleFor(c => c.EmailAddress).EmailAddressRules();
            RuleFor(c => c.Username).UsernameRules();
            RuleFor(c => c.Password).PasswordRules();
        }
    }

    public sealed class Handler(
        IDbContextFactory<EmailDbContext> dbFactory,
        IValidator<Command> validator,
        ISecretProtector protector,
        TimeProvider clock)
    {
        public async Task<Result<EmailAccountDto>> HandleAsync(Command command, CancellationToken cancellationToken)
        {
            var validation = await validator.ValidateAsync(command, cancellationToken).ConfigureAwait(false);
            if (!validation.IsValid)
            {
                return validation.ToError();
            }

            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            var account = await db.Accounts
                .FirstOrDefaultAsync(
                    a => a.Id == command.Id && (a.OwnerId == command.UserId || a.OwnerId == EmailAccount.SystemOwner),
                    cancellationToken)
                .ConfigureAwait(false);
            if (account is null)
            {
                return Error.NotFound("email_account_not_found", "Account not found.");
            }

            var name = command.Name.Trim();
            if (await db.Accounts.AnyAsync(a => a.OwnerId == account.OwnerId && a.Name == name && a.Id != account.Id, cancellationToken).ConfigureAwait(false))
            {
                return Error.Conflict("email_account_conflict", $"An account named '{name}' already exists.");
            }

            var username = EmailAccountRules.NormalizeOptional(command.Username);

            // Keeping the stored password is only safe while it still goes to the same server and login.
            if (username is not null && string.IsNullOrEmpty(command.Password) && account.ProtectedPassword is not null
                && !EmailAccountRules.SameServer(account, command.Host, command.Port, username))
            {
                return Error.Validation("email_account_password_required", "Enter the password again when the server or username is changed.");
            }

            account.Name = name;
            account.Protocol = command.Protocol;
            account.Host = command.Host.Trim();
            account.Port = command.Port;
            account.Security = command.Security;
            account.EmailAddress = command.EmailAddress.Trim();
            account.Username = username;
            if (username is null)
            {
                account.ProtectedPassword = null;
            }
            else if (!string.IsNullOrEmpty(command.Password))
            {
                account.ProtectedPassword = protector.Protect(command.Password);
            }

            account.UpdatedAt = clock.GetUtcNow();

            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return EmailAccountDto.From(account);
        }
    }
}

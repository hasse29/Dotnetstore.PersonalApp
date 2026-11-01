using Dotnetstore.PersonalApp.Modules.Email.Domain;
using Dotnetstore.PersonalApp.Modules.Email.Mail;
using Dotnetstore.PersonalApp.Modules.Email.Persistence;
using Dotnetstore.PersonalApp.Modules.Email.Security;
using Dotnetstore.PersonalApp.Shared.Results;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Email.Features.EmailAccounts;

/// <summary>
/// Tries the settings from the editor (saved or not) by connecting and logging in. When editing an existing account with an
/// empty password, the stored password is used, so the user does not have to retype it just to test.
/// </summary>
internal static class TestEmailConnection
{
    public sealed record Command(
        Guid UserId,
        Guid? AccountId,
        MailProtocol Protocol,
        string Host,
        int Port,
        ConnectionSecurity Security,
        string? Username,
        string? Password);

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(c => c.UserId).UserIdRules();
            RuleFor(c => c.Protocol).ProtocolRules();
            RuleFor(c => c.Host).HostRules();
            RuleFor(c => c.Port).PortRules();
            RuleFor(c => c.Security).SecurityRules();
            RuleFor(c => c.Username).UsernameRules();
            RuleFor(c => c.Password).PasswordRules();
        }
    }

    public sealed class Handler(
        IDbContextFactory<EmailDbContext> dbFactory,
        IValidator<Command> validator,
        ISecretProtector protector,
        IMailGateway gateway)
    {
        public async Task<Result<bool>> HandleAsync(Command command, CancellationToken cancellationToken)
        {
            var validation = await validator.ValidateAsync(command, cancellationToken).ConfigureAwait(false);
            if (!validation.IsValid)
            {
                return validation.ToError();
            }

            var username = EmailAccountRules.NormalizeOptional(command.Username);
            var password = command.Password;

            if (username is not null && string.IsNullOrEmpty(password) && command.AccountId is { } accountId)
            {
                await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
                var found = await EmailAccountAccess.FindAsync(db, accountId, command.UserId, cancellationToken).ConfigureAwait(false);
                if (!found.IsSuccess)
                {
                    return found.Error;
                }

                // The stored password may only go to the server it was saved for, or anyone could send it to a host of their choosing.
                if (!EmailAccountRules.SameServer(found.Value, command.Host, command.Port, username))
                {
                    return Error.Validation("email_account_password_required", "Enter the password again when the server or username is changed.");
                }

                var stored = EmailAccountAccess.DecryptPassword(found.Value, protector);
                if (!stored.IsSuccess)
                {
                    return stored.Error;
                }

                password = stored.Value;
            }

            var server = new MailServer(command.Protocol, command.Host.Trim(), command.Port, command.Security, username, password);

            try
            {
                await gateway.TestAsync(server, cancellationToken).ConfigureAwait(false);
                return true;
            }
            catch (MailGatewayException ex)
            {
                return Error.Validation(ex.Code, ex.Message);
            }
        }
    }
}

using Dotnetstore.PersonalApp.Modules.Email.Domain;
using Dotnetstore.PersonalApp.Modules.Email.Persistence;
using Dotnetstore.PersonalApp.Modules.Email.Security;
using Dotnetstore.PersonalApp.Shared.Results;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Dotnetstore.PersonalApp.Shared.Telemetry;

namespace Dotnetstore.PersonalApp.Modules.Email.Features.EmailAccounts;

/// <summary>Adds a personal account for <c>UserId</c>, or, with <c>IsSystem</c>, a system account that every user can use.</summary>
internal static class CreateEmailAccount
{
    public sealed record Command(
        Guid UserId,
        bool IsSystem,
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
        public Task<Result<EmailAccountDto>> HandleAsync(Command command, CancellationToken cancellationToken) =>
            AppTelemetry.TraceAsync(this, () => HandleCoreAsync(command, cancellationToken), command);

        private async Task<Result<EmailAccountDto>> HandleCoreAsync(Command command, CancellationToken cancellationToken)
        {
            var validation = await validator.ValidateAsync(command, cancellationToken).ConfigureAwait(false);
            if (!validation.IsValid)
            {
                return validation.ToError();
            }

            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            var ownerId = command.IsSystem ? EmailAccount.SystemOwner : command.UserId;
            var name = command.Name.Trim();
            if (await db.Accounts.AnyAsync(a => a.OwnerId == ownerId && a.Name == name, cancellationToken).ConfigureAwait(false))
            {
                return Error.Conflict("email_account_conflict", $"An account named '{name}' already exists.");
            }

            var username = EmailAccountRules.NormalizeOptional(command.Username);
            var now = clock.GetUtcNow();
            var account = new EmailAccount
            {
                Id = Guid.CreateVersion7(),
                OwnerId = ownerId,
                Name = name,
                Protocol = command.Protocol,
                Host = command.Host.Trim(),
                Port = command.Port,
                Security = command.Security,
                EmailAddress = command.EmailAddress.Trim(),
                Username = username,
                // A password without a login would never be used, so it is not kept.
                ProtectedPassword = username is not null && !string.IsNullOrEmpty(command.Password) ? protector.Protect(command.Password) : null,
                CreatedAt = now,
                UpdatedAt = now,
            };

            db.Accounts.Add(account);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return EmailAccountDto.From(account);
        }
    }
}

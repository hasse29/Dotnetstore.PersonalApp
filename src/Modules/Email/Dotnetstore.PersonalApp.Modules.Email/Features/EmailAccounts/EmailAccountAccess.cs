using System.Security.Cryptography;
using Dotnetstore.PersonalApp.Modules.Email.Domain;
using Dotnetstore.PersonalApp.Modules.Email.Mail;
using Dotnetstore.PersonalApp.Modules.Email.Persistence;
using Dotnetstore.PersonalApp.Modules.Email.Security;
using Dotnetstore.PersonalApp.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Email.Features.EmailAccounts;

/// <summary>What the handlers that use an account to talk to a server have in common.</summary>
internal static class EmailAccountAccess
{
    /// <summary>The account if the user may use it (their own or a system account), otherwise a not-found error.</summary>
    public static async Task<Result<EmailAccount>> FindAsync(EmailDbContext db, Guid accountId, Guid userId, CancellationToken cancellationToken)
    {
        if (userId == Guid.Empty)
        {
            return Error.Validation("email_account_user_required", "Nobody is signed in.");
        }

        var account = await db.Accounts
            .FirstOrDefaultAsync(a => a.Id == accountId && (a.OwnerId == userId || a.OwnerId == EmailAccount.SystemOwner), cancellationToken)
            .ConfigureAwait(false);

        return account is null ? Error.NotFound("email_account_not_found", "Account not found.") : account;
    }

    public static Result<MailServer> ToServer(EmailAccount account, ISecretProtector protector)
    {
        var password = DecryptPassword(account, protector);
        return password.IsSuccess
            ? new MailServer(account.Protocol, account.Host, account.Port, account.Security, account.Username, password.Value)
            : password.Error;
    }

    public static Result<string?> DecryptPassword(EmailAccount account, ISecretProtector protector)
    {
        if (string.IsNullOrEmpty(account.ProtectedPassword))
        {
            return Result<string?>.Success(null);
        }

        try
        {
            return Result<string?>.Success(protector.Unprotect(account.ProtectedPassword));
        }
        catch (CryptographicException)
        {
            // DPAPI data belongs to one Windows account on one machine, e.g. after the database was copied elsewhere.
            return Error.Validation("email_password_unreadable", "The stored password cannot be read here. Enter it again.");
        }
    }
}

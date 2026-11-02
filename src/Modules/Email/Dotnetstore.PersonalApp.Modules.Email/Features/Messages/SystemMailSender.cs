using Dotnetstore.PersonalApp.Modules.Email.Domain;
using Dotnetstore.PersonalApp.Modules.Email.Persistence;
using Dotnetstore.PersonalApp.Shared.Auth;
using Dotnetstore.PersonalApp.Shared.Email;
using Dotnetstore.PersonalApp.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Email.Features.Messages;

/// <summary>Sends mail for other modules through the system SMTP account (the first one by name if there are several).</summary>
internal sealed class SystemMailSender(
    IDbContextFactory<EmailDbContext> dbFactory,
    SendEmail.Handler send,
    ICurrentUser currentUser) : ISystemMailSender
{
    public async Task<Result<int>> SendAsync(SystemMail mail, CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        var accountId = await db.Accounts.AsNoTracking()
            .Where(a => a.OwnerId == EmailAccount.SystemOwner && a.Protocol == MailProtocol.Smtp)
            .OrderBy(a => a.Name)
            .Select(a => (Guid?)a.Id)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
        if (accountId is null)
        {
            return Error.NotFound("email_no_system_smtp", "No system SMTP account is registered.");
        }

        var attachments = mail.Attachments.Select(a => new AttachmentInput(a.FileName, a.Content.ToArray())).ToList();

        return await send.HandleAsync(
            new SendEmail.Command(currentUser.User?.Id ?? Guid.Empty, accountId.Value, mail.To, mail.Subject, mail.Body, Attachments: attachments),
            cancellationToken).ConfigureAwait(false);
    }
}

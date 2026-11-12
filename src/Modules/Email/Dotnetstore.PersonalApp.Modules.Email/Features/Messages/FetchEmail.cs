using Dotnetstore.PersonalApp.Modules.Email.Domain;
using Dotnetstore.PersonalApp.Modules.Email.Features.EmailAccounts;
using Dotnetstore.PersonalApp.Modules.Email.Mail;
using Dotnetstore.PersonalApp.Modules.Email.Persistence;
using Dotnetstore.PersonalApp.Modules.Email.Security;
using Dotnetstore.PersonalApp.Shared.Results;
using Microsoft.EntityFrameworkCore;
using Dotnetstore.PersonalApp.Shared.Telemetry;

namespace Dotnetstore.PersonalApp.Modules.Email.Features.Messages;

/// <summary>
/// Downloads new messages from a POP3 account into the local inbox. Messages already stored, or deleted locally, are skipped and
/// nothing is deleted on the server. At most <see cref="MaxPerFetch"/> of the newest new messages come in per call; fetching
/// again brings the next. Attachments over the size limit are left out.
/// </summary>
internal static class FetchEmail
{
    public const int MaxPerFetch = 50;

    public sealed record Command(Guid UserId, Guid AccountId);

    public sealed class Handler(
        IDbContextFactory<EmailDbContext> dbFactory,
        ISecretProtector protector,
        IMailGateway gateway,
        TimeProvider clock)
    {
        /// <summary>Returns the number of new messages stored.</summary>
        public Task<Result<int>> HandleAsync(Command command, CancellationToken cancellationToken) =>
            AppTelemetry.TraceAsync(this, () => HandleCoreAsync(command, cancellationToken), command);

        private async Task<Result<int>> HandleCoreAsync(Command command, CancellationToken cancellationToken)
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            var found = await EmailAccountAccess.FindAsync(db, command.AccountId, command.UserId, cancellationToken).ConfigureAwait(false);
            if (!found.IsSuccess)
            {
                return found.Error;
            }

            var account = found.Value;
            if (account.Protocol != MailProtocol.Pop3)
            {
                return Error.Validation("email_account_not_pop3", "Only POP3 accounts can fetch mail.");
            }

            var server = EmailAccountAccess.ToServer(account, protector);
            if (!server.IsSuccess)
            {
                return server.Error;
            }

            var known = (await db.Messages.Where(m => m.AccountId == account.Id).Select(m => m.Uid).ToListAsync(cancellationToken).ConfigureAwait(false))
                .Concat(await db.DeletedMessages.Where(m => m.AccountId == account.Id).Select(m => m.Uid).ToListAsync(cancellationToken).ConfigureAwait(false))
                .ToHashSet(StringComparer.Ordinal);

            IReadOnlyList<FetchedMessage> fetched;
            try
            {
                fetched = await gateway.FetchAsync(server.Value, known, MaxPerFetch, cancellationToken).ConfigureAwait(false);
            }
            catch (MailGatewayException ex)
            {
                return Error.Validation(ex.Code, ex.Message);
            }

            var now = clock.GetUtcNow();
            var count = 0;
            foreach (var message in fetched.Where(m => known.Add(MailLimits.NormalizeUid(m.Uid))))
            {
                var stored = new ReceivedMessage
                {
                    Id = Guid.CreateVersion7(),
                    AccountId = account.Id,
                    Uid = MailLimits.NormalizeUid(message.Uid),
                    From = Truncate(message.From, 500),
                    Subject = Truncate(message.Subject, 1000),
                    SentAt = message.SentAt,
                    Body = message.Body,
                    ToAddresses = Truncate(string.Join(", ", message.To ?? []), 5000),
                    CcAddresses = Truncate(string.Join(", ", message.Cc ?? []), 5000),
                    HtmlBody = message.HtmlBody,
                    FetchedAt = now,
                };
                db.Messages.Add(stored);
                count++;

                foreach (var attachment in message.Attachments.Take(MailLimits.MaxAttachments).Where(a => a.Content.Length <= MailLimits.MaxAttachmentBytes))
                {
                    db.Attachments.Add(new MailAttachment
                    {
                        Id = Guid.CreateVersion7(),
                        ReceivedMessageId = stored.Id,
                        FileName = Truncate(attachment.FileName, 255),
                        ContentType = Truncate(attachment.ContentType, 200),
                        Content = attachment.Content,
                    });
                }
            }

            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return count;
        }

        private static string Truncate(string value, int maxLength) => value.Length > maxLength ? value[..maxLength] : value;
    }
}

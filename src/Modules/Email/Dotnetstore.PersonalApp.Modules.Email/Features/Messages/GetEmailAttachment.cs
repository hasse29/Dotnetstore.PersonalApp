using Dotnetstore.PersonalApp.Modules.Email.Features.EmailAccounts;
using Dotnetstore.PersonalApp.Modules.Email.Persistence;
using Dotnetstore.PersonalApp.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Email.Features.Messages;

/// <summary>Returns the bytes of one attachment, of a received or a sent message, to anyone who may use the message's account.</summary>
internal static class GetEmailAttachment
{
    public sealed record Query(Guid Id, Guid UserId);

    public sealed record Response(string FileName, string ContentType, byte[] Content);

    public sealed class Handler(IDbContextFactory<EmailDbContext> dbFactory)
    {
        public async Task<Result<Response>> HandleAsync(Query query, CancellationToken cancellationToken)
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

            var attachment = await db.Attachments.AsNoTracking().FirstOrDefaultAsync(a => a.Id == query.Id, cancellationToken).ConfigureAwait(false);
            if (attachment is null)
            {
                return Error.NotFound("email_attachment_not_found", "Attachment not found.");
            }

            if (attachment.DraftId is { } draftId)
            {
                // Drafts are personal: only their owner may read the attachments, whatever account they were written for.
                var owned = await db.Drafts.AnyAsync(d => d.Id == draftId && d.UserId == query.UserId, cancellationToken).ConfigureAwait(false);
                return owned
                    ? new Response(attachment.FileName, attachment.ContentType, attachment.Content)
                    : Error.NotFound("email_attachment_not_found", "Attachment not found.");
            }

            var accountId = attachment.ReceivedMessageId is { } receivedId
                ? await db.Messages.Where(m => m.Id == receivedId).Select(m => (Guid?)m.AccountId).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false)
                : await db.SentMessages.Where(m => m.Id == attachment.SentMessageId).Select(m => (Guid?)m.AccountId).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);

            var account = accountId is { } id
                ? await EmailAccountAccess.FindAsync(db, id, query.UserId, cancellationToken).ConfigureAwait(false)
                : null;
            return account is { IsSuccess: true }
                ? new Response(attachment.FileName, attachment.ContentType, attachment.Content)
                : Error.NotFound("email_attachment_not_found", "Attachment not found.");
        }
    }
}

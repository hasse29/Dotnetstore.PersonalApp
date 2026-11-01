using Dotnetstore.PersonalApp.Modules.Email.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Email.Features.Messages;

/// <summary>Lists a message's attachments without loading their bytes (the size is computed by the database).</summary>
internal static class AttachmentQueries
{
    public static async Task<IReadOnlyList<EmailAttachmentDto>> OfReceivedAsync(EmailDbContext db, Guid messageId, CancellationToken cancellationToken) =>
        await db.Attachments.AsNoTracking()
            .Where(a => a.ReceivedMessageId == messageId)
            .OrderBy(a => a.FileName)
            .Select(a => new EmailAttachmentDto(a.Id, a.FileName, a.ContentType, a.Content.Length))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public static async Task<IReadOnlyList<EmailAttachmentDto>> OfDraftAsync(EmailDbContext db, Guid draftId, CancellationToken cancellationToken) =>
        await db.Attachments.AsNoTracking()
            .Where(a => a.DraftId == draftId)
            .OrderBy(a => a.FileName)
            .Select(a => new EmailAttachmentDto(a.Id, a.FileName, a.ContentType, a.Content.Length))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public static async Task<IReadOnlyList<EmailAttachmentDto>> OfSentAsync(EmailDbContext db, Guid messageId, CancellationToken cancellationToken) =>
        await db.Attachments.AsNoTracking()
            .Where(a => a.SentMessageId == messageId)
            .OrderBy(a => a.FileName)
            .Select(a => new EmailAttachmentDto(a.Id, a.FileName, a.ContentType, a.Content.Length))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
}

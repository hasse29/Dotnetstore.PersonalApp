using Dotnetstore.PersonalApp.Modules.Email.Domain;

namespace Dotnetstore.PersonalApp.Modules.Email.Features.Messages;

/// <summary>An attachment as listed on a message; the bytes are fetched on demand by <c>GetEmailAttachment</c>.</summary>
internal sealed record EmailAttachmentDto(Guid Id, string FileName, string ContentType, int Size);

/// <summary>
/// A downloaded message. <see cref="Body"/>, <see cref="HtmlBody"/> and <see cref="Attachments"/> are empty in lists;
/// <c>GetEmailMessage</c> returns them in full. <see cref="To"/> and <see cref="Cc"/> are plain addresses, comma separated.
/// </summary>
internal sealed record EmailMessageDto(
    Guid Id,
    Guid AccountId,
    string From,
    string To,
    string Cc,
    string Subject,
    DateTimeOffset SentAt,
    string Body,
    string? HtmlBody,
    IReadOnlyList<EmailAttachmentDto> Attachments)
{
    public static EmailMessageDto Summary(ReceivedMessage message) =>
        new(message.Id, message.AccountId, message.From, message.ToAddresses, message.CcAddresses, message.Subject, message.SentAt, string.Empty, null, []);

    public static EmailMessageDto Full(ReceivedMessage message, IReadOnlyList<EmailAttachmentDto> attachments) =>
        new(message.Id, message.AccountId, message.From, message.ToAddresses, message.CcAddresses, message.Subject, message.SentAt, message.Body, message.HtmlBody, attachments);
}

/// <summary>A copy of a sent message, with the same list/full split as <see cref="EmailMessageDto"/>.</summary>
internal sealed record SentMessageDto(
    Guid Id,
    Guid AccountId,
    string To,
    string Cc,
    string Subject,
    DateTimeOffset SentAt,
    string Body,
    bool IsHtml,
    IReadOnlyList<EmailAttachmentDto> Attachments)
{
    public static SentMessageDto Summary(SentMessage message) =>
        new(message.Id, message.AccountId, message.To, message.Cc, message.Subject, message.SentAt, string.Empty, message.IsHtml, []);

    public static SentMessageDto Full(SentMessage message, IReadOnlyList<EmailAttachmentDto> attachments) =>
        new(message.Id, message.AccountId, message.To, message.Cc, message.Subject, message.SentAt, message.Body, message.IsHtml, attachments);
}

/// <summary>A saved draft, with the same list/full split as <see cref="EmailMessageDto"/>.</summary>
internal sealed record DraftDto(
    Guid Id,
    Guid? AccountId,
    string To,
    string Cc,
    string Subject,
    string Body,
    bool IsHtml,
    DateTimeOffset UpdatedAt,
    IReadOnlyList<EmailAttachmentDto> Attachments)
{
    public static DraftDto Summary(Draft draft) =>
        new(draft.Id, draft.AccountId, draft.To, draft.Cc, draft.Subject, string.Empty, draft.IsHtml, draft.UpdatedAt, []);

    public static DraftDto Full(Draft draft, IReadOnlyList<EmailAttachmentDto> attachments) =>
        new(draft.Id, draft.AccountId, draft.To, draft.Cc, draft.Subject, draft.Body, draft.IsHtml, draft.UpdatedAt, attachments);
}

/// <summary>A file the user picked to attach to a message.</summary>
internal sealed record AttachmentInput(string FileName, byte[] Content);

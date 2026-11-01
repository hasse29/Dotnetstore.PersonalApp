namespace Dotnetstore.PersonalApp.Modules.Email.Domain;

/// <summary>A file attached to a received message, a sent message or a draft; exactly one of the three ids is set. Goes with its message.</summary>
internal sealed class MailAttachment
{
    public Guid Id { get; set; }

    public Guid? ReceivedMessageId { get; set; }

    public Guid? SentMessageId { get; set; }

    public Guid? DraftId { get; set; }

    public string FileName { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;

    public byte[] Content { get; set; } = [];
}

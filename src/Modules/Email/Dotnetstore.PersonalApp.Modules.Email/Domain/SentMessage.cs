namespace Dotnetstore.PersonalApp.Modules.Email.Domain;

/// <summary>A copy of a message that was sent through an SMTP account. Belongs to the account and goes with it.</summary>
internal sealed class SentMessage
{
    public Guid Id { get; set; }

    public Guid AccountId { get; set; }

    /// <summary>The recipients, comma separated.</summary>
    public string To { get; set; } = string.Empty;

    /// <summary>The Cc recipients, comma separated; empty for none.</summary>
    public string Cc { get; set; } = string.Empty;

    public string Subject { get; set; } = string.Empty;

    /// <summary>The text, or the HTML source when <see cref="IsHtml"/> is set.</summary>
    public string Body { get; set; } = string.Empty;

    public bool IsHtml { get; set; }

    public DateTimeOffset SentAt { get; set; }
}

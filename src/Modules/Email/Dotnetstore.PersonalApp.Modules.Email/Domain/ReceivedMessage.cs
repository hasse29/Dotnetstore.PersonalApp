namespace Dotnetstore.PersonalApp.Modules.Email.Domain;

/// <summary>
/// A message downloaded from a POP3 account. Messages belong to the account, so everyone who may use a system account sees
/// the same inbox, and they are deleted with the account. The server's copy is left untouched.
/// </summary>
internal sealed class ReceivedMessage
{
    public Guid Id { get; set; }

    public Guid AccountId { get; set; }

    /// <summary>The server's unique id (POP3 UIDL); with <see cref="AccountId"/> it stops a message from being stored twice.</summary>
    public string Uid { get; set; } = string.Empty;

    public string From { get; set; } = string.Empty;

    public string Subject { get; set; } = string.Empty;

    public DateTimeOffset SentAt { get; set; }

    /// <summary>The text of the message (HTML is reduced to text), cut at 100 000 characters.</summary>
    public string Body { get; set; } = string.Empty;

    /// <summary>The recipients in the To and Cc headers as plain addresses, comma separated; empty for mail fetched before they were kept.</summary>
    public string ToAddresses { get; set; } = string.Empty;

    public string CcAddresses { get; set; } = string.Empty;

    /// <summary>The HTML part of the message, shown formatted but never executed or loaded from the web; null when there is none. Cut at 200 000 characters.</summary>
    public string? HtmlBody { get; set; }

    public DateTimeOffset FetchedAt { get; set; }
}

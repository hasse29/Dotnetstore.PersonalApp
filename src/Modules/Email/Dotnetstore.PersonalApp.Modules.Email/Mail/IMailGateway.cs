using System.Diagnostics.CodeAnalysis;
using Dotnetstore.PersonalApp.Modules.Email.Domain;

namespace Dotnetstore.PersonalApp.Modules.Email.Mail;

/// <summary>Where to connect and how to log in. The password is plain text here, so instances must not be stored or logged.</summary>
internal sealed record MailServer(MailProtocol Protocol, string Host, int Port, ConnectionSecurity Security, string? Username, string? Password);

/// <summary>A file travelling with a message; the content type is a MIME type such as <c>application/pdf</c>.</summary>
internal sealed record MailAttachmentData(string FileName, string ContentType, byte[] Content);

internal sealed record OutgoingMessage(string From, IReadOnlyList<string> To, string Subject, string Body, bool IsHtml, IReadOnlyList<MailAttachmentData> Attachments, IReadOnlyList<string>? Cc = null);

internal sealed record FetchedMessage(string Uid, string From, string Subject, DateTimeOffset SentAt, string Body, IReadOnlyList<MailAttachmentData> Attachments, IReadOnlyList<string>? To = null, IReadOnlyList<string>? Cc = null, string? HtmlBody = null);

/// <summary>
/// A failed mail operation, already sorted into a stable <see cref="Code"/> (e.g. <c>email_authentication_failed</c>) that the UI
/// translates; <see cref="Exception.Message"/> carries the server's or network's own text for the English fallback.
/// </summary>
[SuppressMessage("Design", "CA1032:Implement standard exception constructors", Justification = "Only thrown and caught inside the module, always with a code.")]
[SuppressMessage("Design", "CA1064:Exceptions should be public", Justification = "Module internals stay internal (CA1515).")]
internal sealed class MailGatewayException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;

    public static bool IsGatewayCode(string code) =>
        code is "email_authentication_failed" or "email_tls_failed" or "email_connection_failed" or "email_server_error";
}

/// <summary>The only place that talks to mail servers, so everything else can be tested without a network.</summary>
internal interface IMailGateway
{
    /// <summary>Connects and logs in, then disconnects. Throws <see cref="MailGatewayException"/> when that fails.</summary>
    Task TestAsync(MailServer server, CancellationToken cancellationToken);

    Task SendAsync(MailServer server, OutgoingMessage message, CancellationToken cancellationToken);

    /// <summary>
    /// Downloads up to <paramref name="maxMessages"/> of the newest messages whose ids are not in <paramref name="knownUids"/>.
    /// Nothing is deleted from the server.
    /// </summary>
    Task<IReadOnlyList<FetchedMessage>> FetchAsync(
        MailServer server,
        IReadOnlySet<string> knownUids,
        int maxMessages,
        CancellationToken cancellationToken);
}

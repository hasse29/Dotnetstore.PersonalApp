using System.Net.Sockets;
using System.Text.RegularExpressions;
using Dotnetstore.PersonalApp.Modules.Email.Domain;
using MailKit;
using MailKit.Net.Pop3;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace Dotnetstore.PersonalApp.Modules.Email.Mail;

internal sealed partial class MailKitGateway : IMailGateway
{
    public const int MaxBodyLength = 100_000;

    private const int TimeoutMilliseconds = 20_000;

    public async Task TestAsync(MailServer server, CancellationToken cancellationToken)
    {
        if (server.Protocol == MailProtocol.Smtp)
        {
            await WithSmtpAsync(server, _ => Task.CompletedTask, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            await WithPop3Async(server, _ => Task.CompletedTask, cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task SendAsync(MailServer server, OutgoingMessage message, CancellationToken cancellationToken)
    {
        using var mime = new MimeMessage();
        mime.From.Add(MailboxAddress.Parse(message.From));
        foreach (var to in message.To)
        {
            mime.To.Add(MailboxAddress.Parse(to));
        }

        foreach (var cc in message.Cc ?? [])
        {
            mime.Cc.Add(MailboxAddress.Parse(cc));
        }

        mime.Subject = message.Subject;
        var builder = new BodyBuilder();
        if (message.IsHtml)
        {
            builder.HtmlBody = message.Body;
        }
        else
        {
            builder.TextBody = message.Body;
        }

        foreach (var attachment in message.Attachments)
        {
            builder.Attachments.Add(new MimePart(ContentType.Parse(attachment.ContentType))
            {
                Content = new MimeContent(new MemoryStream(attachment.Content)),
                ContentDisposition = new ContentDisposition(ContentDisposition.Attachment),
                ContentTransferEncoding = ContentEncoding.Base64,
                FileName = attachment.FileName,
            });
        }

        mime.Body = builder.ToMessageBody();

        await WithSmtpAsync(server, client => client.SendAsync(mime, cancellationToken), cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<FetchedMessage>> FetchAsync(
        MailServer server,
        IReadOnlySet<string> knownUids,
        int maxMessages,
        CancellationToken cancellationToken)
    {
        var fetched = new List<FetchedMessage>();

        await WithPop3Async(
            server,
            async client =>
            {
                var uids = await client.GetMessageUidsAsync(cancellationToken).ConfigureAwait(false);

                // Newest first (POP3 numbers messages in arrival order), so a big mailbox shows the latest mail before the old.
                var wanted = Enumerable.Range(0, uids.Count)
                    .Where(index => !knownUids.Contains(MailLimits.NormalizeUid(uids[index])))
                    .Reverse()
                    .Take(maxMessages);

                foreach (var index in wanted)
                {
                    var mime = await client.GetMessageAsync(index, cancellationToken).ConfigureAwait(false);
                    fetched.Add(new FetchedMessage(uids[index], mime.From.ToString(), mime.Subject ?? string.Empty, mime.Date, BodyOf(mime), AttachmentsOf(mime), AddressesOf(mime.To), AddressesOf(mime.Cc), HtmlOf(mime)));
                }
            },
            cancellationToken).ConfigureAwait(false);

        return fetched;
    }

    private static async Task WithSmtpAsync(MailServer server, Func<SmtpClient, Task> action, CancellationToken cancellationToken)
    {
        await RunAsync(
            async () =>
            {
                using var client = new SmtpClient { Timeout = TimeoutMilliseconds };
                await client.ConnectAsync(server.Host, server.Port, Options(server.Security), cancellationToken).ConfigureAwait(false);
                if (!string.IsNullOrEmpty(server.Username))
                {
                    await client.AuthenticateAsync(server.Username, server.Password ?? string.Empty, cancellationToken).ConfigureAwait(false);
                }

                await action(client).ConfigureAwait(false);
                await client.DisconnectAsync(quit: true, cancellationToken).ConfigureAwait(false);
            },
            cancellationToken).ConfigureAwait(false);
    }

    private static async Task WithPop3Async(MailServer server, Func<Pop3Client, Task> action, CancellationToken cancellationToken)
    {
        await RunAsync(
            async () =>
            {
                using var client = new Pop3Client { Timeout = TimeoutMilliseconds };
                await client.ConnectAsync(server.Host, server.Port, Options(server.Security), cancellationToken).ConfigureAwait(false);
                if (!string.IsNullOrEmpty(server.Username))
                {
                    await client.AuthenticateAsync(server.Username, server.Password ?? string.Empty, cancellationToken).ConfigureAwait(false);
                }

                await action(client).ConfigureAwait(false);
                await client.DisconnectAsync(quit: true, cancellationToken).ConfigureAwait(false);
            },
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Turns the many ways a mail connection can fail into <see cref="MailGatewayException"/>s; cancellation passes through.</summary>
    private static async Task RunAsync(Func<Task> operation, CancellationToken cancellationToken)
    {
        try
        {
            await operation().ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (AuthenticationException ex)
        {
            throw new MailGatewayException("email_authentication_failed", ex.Message);
        }
        catch (SslHandshakeException ex)
        {
            throw new MailGatewayException("email_tls_failed", ex.Message);
        }
        catch (Exception ex) when (ex is SocketException or IOException or TimeoutException or OperationCanceledException or ServiceNotConnectedException)
        {
            throw new MailGatewayException("email_connection_failed", ex.Message);
        }
        catch (Exception ex) when (ex is ProtocolException or CommandException or ServiceNotAuthenticatedException or ParseException
            or NotSupportedException or InvalidOperationException or ObjectDisposedException or FormatException or ArgumentException
            or System.Security.Authentication.AuthenticationException)
        {
            throw new MailGatewayException("email_server_error", ex.Message);
        }
    }

    private static SecureSocketOptions Options(ConnectionSecurity security) => security switch
    {
        ConnectionSecurity.None => SecureSocketOptions.None,
        ConnectionSecurity.StartTls => SecureSocketOptions.StartTls,
        _ => SecureSocketOptions.SslOnConnect,
    };

    private static List<MailAttachmentData> AttachmentsOf(MimeMessage mime)
    {
        var result = new List<MailAttachmentData>();
        foreach (var entity in mime.Attachments)
        {
            if (result.Count >= MailLimits.MaxAttachments)
            {
                break;
            }

            using var buffer = new MemoryStream();
            string name;
            string type;
            switch (entity)
            {
                case MimePart part:
                    name = part.FileName ?? "attachment";
                    type = part.ContentType.MimeType;
                    part.Content?.DecodeTo(buffer);
                    break;
                case MessagePart forwarded:
                    name = forwarded.ContentDisposition?.FileName ?? "message.eml";
                    type = "message/rfc822";
                    forwarded.Message?.WriteTo(buffer);
                    break;
                default:
                    continue;
            }

            if (buffer.Length <= MailLimits.MaxAttachmentBytes)
            {
                result.Add(new MailAttachmentData(name, type, buffer.ToArray()));
            }
        }

        return result;
    }

    private static List<string> AddressesOf(InternetAddressList list) => [.. list.Mailboxes.Select(m => m.Address)];

    private static string? HtmlOf(MimeMessage mime)
    {
        var html = mime.HtmlBody;
        return html is { Length: > MailLimits.MaxHtmlLength } ? html[..MailLimits.MaxHtmlLength] : html;
    }

    private static string BodyOf(MimeMessage mime)
    {
        var text = mime.TextBody;
        if (text is null && mime.HtmlBody is { } html)
        {
            text = HtmlTag().Replace(html, string.Empty);
        }

        text ??= string.Empty;
        return text.Length > MaxBodyLength ? text[..MaxBodyLength] : text;
    }

    [GeneratedRegex("<[^>]*>")]
    private static partial Regex HtmlTag();
}

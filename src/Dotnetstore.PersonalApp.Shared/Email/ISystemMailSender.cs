using Dotnetstore.PersonalApp.Shared.Results;

namespace Dotnetstore.PersonalApp.Shared.Email;

/// <summary>A file to attach to a mail.</summary>
public sealed record MailFile(string FileName, ReadOnlyMemory<byte> Content);

/// <summary>A plain text mail to send from the system's SMTP account.</summary>
public sealed record SystemMail(string To, string Subject, string Body, IReadOnlyList<MailFile> Attachments);

/// <summary>
/// Implemented by the Email module so that other modules can send mail without referencing it. Mail always goes out through the
/// registered system SMTP account (error <c>email_no_system_smtp</c> when there is none).
/// </summary>
public interface ISystemMailSender
{
    /// <returns>The number of recipients the message was sent to.</returns>
    Task<Result<int>> SendAsync(SystemMail mail, CancellationToken cancellationToken);
}

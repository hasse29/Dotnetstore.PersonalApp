namespace Dotnetstore.PersonalApp.Modules.Email.Mail;

/// <summary>Size limits for attachments, which are kept in the database: bigger files are refused when sending and skipped when fetching.</summary>
internal static class MailLimits
{
    public const int MaxAttachments = 20;
    public const int MaxAttachmentBytes = 10 * 1024 * 1024;
    public const int MaxTotalAttachmentBytes = 25 * 1024 * 1024;

    /// <summary>The HTML source of a received message is cut here; it is only used to show the message formatted.</summary>
    public const int MaxHtmlLength = 200_000;

    public const int MaxUidLength = 200;

    /// <summary>
    /// The form of a server UIDL that is stored and compared. A UIDL that does not fit is replaced by its hash rather than cut,
    /// because a cut value would never match the server again and two UIDLs with the same start would collide.
    /// </summary>
    public static string NormalizeUid(string uid) =>
        uid.Length <= MaxUidLength
            ? uid
            : "sha256:" + Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(uid)));
}

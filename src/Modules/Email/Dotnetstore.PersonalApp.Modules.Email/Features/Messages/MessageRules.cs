using System.Net.Mail;

namespace Dotnetstore.PersonalApp.Modules.Email.Features.Messages;

internal static class MessageRules
{
    public const int MaxSubjectLength = 500;
    public const int MaxBodyLength = 100_000;
    public const int MaxRecipients = 50;
    public const int MaxRecipientFieldLength = 5000;

    private static readonly char[] RecipientSeparators = [',', ';', ' ', '\n', '\r', '\t'];

    /// <summary>Splits a recipient field on commas, semicolons and whitespace and drops duplicates (ignoring case).</summary>
    public static IReadOnlyList<string> ParseRecipients(string text) =>
        [.. text.Split(RecipientSeparators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)];

    /// <summary>The plain address of a "From" header such as <c>Bo &lt;bo@example.com&gt;</c>; the text itself when it cannot be read.</summary>
    public static string ExtractAddress(string from) =>
        MailAddress.TryCreate(from, out var address) ? address.Address : from.Trim();

    /// <summary>The subject with <paramref name="prefix"/> (e.g. "Re: ") in front, unless it already starts with it.</summary>
    public static string WithPrefix(string prefix, string subject) =>
        subject.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) ? subject : prefix + subject;
}

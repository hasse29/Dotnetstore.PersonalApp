using System.Globalization;
using System.Windows.Input;
using Dotnetstore.PersonalApp.Modules.Email.Features.EmailAccounts;

namespace Dotnetstore.PersonalApp.Modules.Email.Features.Messages;

/// <summary>An account in the inbox, sent mail or composer drop-down.</summary>
internal sealed record AccountChoice(Guid Id, string Label, string EmailAddress)
{
    public static AccountChoice From(EmailAccountDto account) => new(account.Id, $"{account.Name} ({account.EmailAddress})", account.EmailAddress);
}

/// <summary>A file attached to the message being written; the command takes it off again.</summary>
internal sealed record ComposeAttachment(string FileName, byte[] Content, string SizeText, ICommand RemoveCommand);

/// <summary>An attachment of a stored message in the reading pane; the command saves it to disk.</summary>
internal sealed record AttachmentRow(string Label, ICommand SaveCommand);

internal static class SizeText
{
    public static string Format(long bytes) => bytes switch
    {
        < 1024 => string.Create(CultureInfo.CurrentCulture, $"{bytes} B"),
        < 1024 * 1024 => string.Create(CultureInfo.CurrentCulture, $"{bytes / 1024.0:0.#} KB"),
        _ => string.Create(CultureInfo.CurrentCulture, $"{bytes / 1024.0 / 1024.0:0.#} MB"),
    };
}

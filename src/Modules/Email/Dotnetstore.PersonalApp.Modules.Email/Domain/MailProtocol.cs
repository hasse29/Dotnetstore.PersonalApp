namespace Dotnetstore.PersonalApp.Modules.Email.Domain;

/// <summary>What an account is used for: SMTP sends mail, POP3 fetches it.</summary>
internal enum MailProtocol
{
    Smtp,
    Pop3,
}

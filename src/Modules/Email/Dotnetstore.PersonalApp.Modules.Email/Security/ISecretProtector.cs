namespace Dotnetstore.PersonalApp.Modules.Email.Security;

/// <summary>Encrypts secrets (mail passwords) before they are stored, so the database never holds them in plain text.</summary>
internal interface ISecretProtector
{
    string Protect(string plainText);

    string Unprotect(string protectedText);
}

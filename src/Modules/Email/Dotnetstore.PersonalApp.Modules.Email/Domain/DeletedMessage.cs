namespace Dotnetstore.PersonalApp.Modules.Email.Domain;

/// <summary>
/// Remembers that a downloaded message was deleted locally. The server's copy is left alone, so without this the next fetch
/// would download the message again.
/// </summary>
internal sealed class DeletedMessage
{
    public Guid Id { get; set; }

    public Guid AccountId { get; set; }

    public string Uid { get; set; } = string.Empty;
}

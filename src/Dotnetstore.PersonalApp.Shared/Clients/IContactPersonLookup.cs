namespace Dotnetstore.PersonalApp.Shared.Clients;

/// <summary>A client's contact person as other modules see it. The id is the owner id of the person's addresses, phone numbers and email addresses.</summary>
public sealed record ContactPersonReference(Guid Id, string Name);

/// <summary>Implemented by the Clients module so that other modules can pick a client's contact person without referencing it.</summary>
public interface IContactPersonLookup
{
    /// <summary>The client's contact persons ordered by name.</summary>
    Task<IReadOnlyList<ContactPersonReference>> ListAsync(Guid clientId, CancellationToken cancellationToken);
}

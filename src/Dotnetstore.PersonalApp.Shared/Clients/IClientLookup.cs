namespace Dotnetstore.PersonalApp.Shared.Clients;

/// <summary>A client as other modules see it: just enough to pick one and show its name.</summary>
public sealed record ClientReference(Guid Id, string Name);

/// <summary>Implemented by the Clients module so that other modules can refer to clients without referencing it.</summary>
public interface IClientLookup
{
    /// <summary>All clients ordered by name.</summary>
    Task<IReadOnlyList<ClientReference>> ListAsync(CancellationToken cancellationToken);
}
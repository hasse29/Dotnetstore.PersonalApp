namespace Dotnetstore.PersonalApp.Shared.Contracts.Events;

/// <summary>Published by the Clients module after a client has been deleted.</summary>
public sealed record ClientDeleted(Guid ClientId);

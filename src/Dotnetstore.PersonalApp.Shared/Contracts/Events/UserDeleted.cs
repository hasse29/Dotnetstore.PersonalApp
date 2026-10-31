namespace Dotnetstore.PersonalApp.Shared.Contracts.Events;

/// <summary>Published by the Users module after a user has been deleted.</summary>
public sealed record UserDeleted(Guid UserId);

namespace Dotnetstore.PersonalApp.Shared.Contracts.Events;

/// <summary>Published by the Users module after a user's photo was set or removed, so the top bar can follow.</summary>
public sealed record UserPhotoChanged(Guid UserId);

namespace Dotnetstore.PersonalApp.Shared.Contracts.Events;

/// <summary>Published by the Production module after a product has been deleted.</summary>
public sealed record ProductDeleted(Guid ProductId);

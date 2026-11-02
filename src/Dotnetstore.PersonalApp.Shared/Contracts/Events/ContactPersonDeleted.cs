namespace Dotnetstore.PersonalApp.Shared.Contracts.Events;

/// <summary>Published by the Clients module after a client's contact person has been deleted (also once per contact person when the client itself is deleted).</summary>
public sealed record ContactPersonDeleted(Guid ContactPersonId);

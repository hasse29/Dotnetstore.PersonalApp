namespace Dotnetstore.PersonalApp.Shared.Contracts.Events;

/// <summary>Published by the Subjects module after a subject has been deleted.</summary>
public sealed record SubjectDeleted(Guid SubjectId);

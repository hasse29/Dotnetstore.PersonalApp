namespace Dotnetstore.PersonalApp.Shared.Contracts.Events;

/// <summary>Published by the Students module after a student has been deleted (also once per student when their school is deleted).</summary>
public sealed record StudentDeleted(Guid StudentId);

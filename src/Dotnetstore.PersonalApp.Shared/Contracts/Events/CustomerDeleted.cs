namespace Dotnetstore.PersonalApp.Shared.Contracts.Events;

/// <summary>Published by the Sales module after a customer has been deleted, so the modules holding its addresses, email addresses and phone numbers can remove them.</summary>
public sealed record CustomerDeleted(Guid CustomerId);

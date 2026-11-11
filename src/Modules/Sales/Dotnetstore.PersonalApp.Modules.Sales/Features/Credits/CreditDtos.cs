using Dotnetstore.PersonalApp.Modules.Sales.Domain;

namespace Dotnetstore.PersonalApp.Modules.Sales.Features.Credits;

/// <summary>A customer who has credit to use or pay back.</summary>
internal sealed record CustomerCreditDto(Guid CustomerId, string CustomerName, decimal Balance);

internal sealed record CreditEntryDto(Guid Id, CreditEntryKind Kind, DateOnly Date, decimal Amount, string? Note);

internal sealed record CustomerCreditDetailDto(CustomerCreditDto Customer, IReadOnlyList<CreditEntryDto> Entries);

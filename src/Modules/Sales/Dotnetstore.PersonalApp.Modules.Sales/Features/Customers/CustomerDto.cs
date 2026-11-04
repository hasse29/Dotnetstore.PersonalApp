using System.Linq.Expressions;
using Dotnetstore.PersonalApp.Modules.Sales.Domain;
using Dotnetstore.PersonalApp.Modules.Sales.Features;

namespace Dotnetstore.PersonalApp.Modules.Sales.Features.Customers;

internal sealed record CustomerDto(Guid Id, string Number, string Name, Guid? ClientId, string? ClientName, Guid? StoreId, string? StoreName, Guid? TerritoryId, string? TerritoryName)
{
    public static Expression<Func<Customer, CustomerDto>> Projection { get; } =
        t => new CustomerDto(t.Id, t.Number, t.Name, t.ClientId, null, t.StoreId, t.Store == null ? null : t.Store.Name, t.TerritoryId, t.Territory == null ? null : t.Territory.Name);

    public CustomerDto WithNames(IReadOnlyDictionary<Guid, string> clientNames) => this with { ClientName = ExternalReferences.NameOf(clientNames, ClientId) };
}
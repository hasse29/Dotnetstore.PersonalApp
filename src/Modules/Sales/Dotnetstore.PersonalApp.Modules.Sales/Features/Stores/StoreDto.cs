using System.Linq.Expressions;
using Dotnetstore.PersonalApp.Modules.Sales.Domain;

namespace Dotnetstore.PersonalApp.Modules.Sales.Features.Stores;

internal sealed record StoreDto(Guid Id, string Name, Guid? SalesPersonId, string? SalesPersonName)
{
    public static Expression<Func<Store, StoreDto>> Projection { get; } =
        t => new StoreDto(t.Id, t.Name, t.SalesPersonId, t.SalesPerson == null ? null : t.SalesPerson.Name);
}
using System.Linq.Expressions;
using Dotnetstore.PersonalApp.Modules.Purchasing.Domain;

namespace Dotnetstore.PersonalApp.Modules.Purchasing.Features.ShipMethods;

internal sealed record ShipMethodDto(Guid Id, string Name, decimal ShipBase, decimal ShipRate)
{
    public static Expression<Func<ShipMethod, ShipMethodDto>> Projection { get; } =
        t => new ShipMethodDto(t.Id, t.Name, t.ShipBase, t.ShipRate);
}
using System.Linq.Expressions;
using Dotnetstore.PersonalApp.Modules.Production.Domain;

namespace Dotnetstore.PersonalApp.Modules.Production.Features.Locations;

internal sealed record LocationDto(Guid Id, string Name, decimal CostRate, decimal Availability)
{
    public static Expression<Func<Location, LocationDto>> Projection { get; } =
        t => new LocationDto(t.Id, t.Name, t.CostRate, t.Availability);
}
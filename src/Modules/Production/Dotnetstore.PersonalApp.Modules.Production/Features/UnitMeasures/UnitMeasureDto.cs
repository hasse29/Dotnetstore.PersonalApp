using System.Linq.Expressions;
using Dotnetstore.PersonalApp.Modules.Production.Domain;

namespace Dotnetstore.PersonalApp.Modules.Production.Features.UnitMeasures;

internal sealed record UnitMeasureDto(Guid Id, string Code, string Name)
{
    public static Expression<Func<UnitMeasure, UnitMeasureDto>> Projection { get; } =
        t => new UnitMeasureDto(t.Id, t.Code, t.Name);
}
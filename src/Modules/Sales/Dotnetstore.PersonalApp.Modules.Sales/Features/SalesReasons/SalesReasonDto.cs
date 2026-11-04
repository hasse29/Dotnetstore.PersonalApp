using System.Linq.Expressions;
using Dotnetstore.PersonalApp.Modules.Sales.Domain;

namespace Dotnetstore.PersonalApp.Modules.Sales.Features.SalesReasons;

internal sealed record SalesReasonDto(Guid Id, string Name, string ReasonType)
{
    public static Expression<Func<SalesReason, SalesReasonDto>> Projection { get; } =
        t => new SalesReasonDto(t.Id, t.Name, t.ReasonType);
}
using System.Linq.Expressions;
using Dotnetstore.PersonalApp.Modules.Production.Domain;

namespace Dotnetstore.PersonalApp.Modules.Production.Features.ScrapReasons;

internal sealed record ScrapReasonDto(Guid Id, string Name)
{
    public static Expression<Func<ScrapReason, ScrapReasonDto>> Projection { get; } =
        t => new ScrapReasonDto(t.Id, t.Name);
}
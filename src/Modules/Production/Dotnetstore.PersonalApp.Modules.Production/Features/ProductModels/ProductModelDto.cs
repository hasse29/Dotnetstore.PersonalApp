using System.Linq.Expressions;
using Dotnetstore.PersonalApp.Modules.Production.Domain;

namespace Dotnetstore.PersonalApp.Modules.Production.Features.ProductModels;

internal sealed record ProductModelDto(Guid Id, string Name, string? Description, string? Instructions)
{
    public static Expression<Func<ProductModel, ProductModelDto>> Projection { get; } =
        t => new ProductModelDto(t.Id, t.Name, t.Description, t.Instructions);
}
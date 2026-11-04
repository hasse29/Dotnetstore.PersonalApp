using System.Linq.Expressions;
using Dotnetstore.PersonalApp.Modules.Production.Domain;

namespace Dotnetstore.PersonalApp.Modules.Production.Features.BillOfMaterials;

internal sealed record BillOfMaterialDto(Guid Id, Guid? ProductAssemblyId, string? ProductAssemblyName, Guid ComponentId, string ComponentName, DateOnly StartDate, DateOnly? EndDate, Guid UnitId, string UnitName, int BomLevel, decimal PerAssemblyQty)
{
    public static Expression<Func<BillOfMaterial, BillOfMaterialDto>> Projection { get; } =
        t => new BillOfMaterialDto(t.Id, t.ProductAssemblyId, t.ProductAssembly == null ? null : t.ProductAssembly.Name, t.ComponentId, t.Component.Name, t.StartDate, t.EndDate, t.UnitId, t.Unit.Name, t.BomLevel, t.PerAssemblyQty);
}
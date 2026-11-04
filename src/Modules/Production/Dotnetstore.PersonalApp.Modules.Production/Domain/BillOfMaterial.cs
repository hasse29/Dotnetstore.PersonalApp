namespace Dotnetstore.PersonalApp.Modules.Production.Domain;

/// <summary>bill of materials line.</summary>
internal sealed class BillOfMaterial
{
    public Guid Id { get; set; }

    public Guid? ProductAssemblyId { get; set; }

    public Product? ProductAssembly { get; set; }

    public Guid ComponentId { get; set; }

    public Product Component { get; set; } = null!;

    public DateOnly StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public Guid UnitId { get; set; }

    public UnitMeasure Unit { get; set; } = null!;

    public int BomLevel { get; set; }

    public decimal PerAssemblyQty { get; set; }
}
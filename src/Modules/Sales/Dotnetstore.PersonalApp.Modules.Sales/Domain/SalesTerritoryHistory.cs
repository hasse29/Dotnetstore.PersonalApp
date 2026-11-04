namespace Dotnetstore.PersonalApp.Modules.Sales.Domain;

/// <summary>territory assignment.</summary>
internal sealed class SalesTerritoryHistory
{
    public Guid Id { get; set; }

    public Guid SalesPersonId { get; set; }

    public SalesPerson SalesPerson { get; set; } = null!;

    public Guid TerritoryId { get; set; }

    public SalesTerritory Territory { get; set; } = null!;

    public DateOnly StartDate { get; set; }

    public DateOnly? EndDate { get; set; }
}
namespace Dotnetstore.PersonalApp.Modules.Sales.Domain;

/// <summary>salesperson.</summary>
internal sealed class SalesPerson
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public Guid? UserId { get; set; }

    public Guid? TerritoryId { get; set; }

    public SalesTerritory? Territory { get; set; }

    public decimal? SalesQuota { get; set; }

    public decimal Bonus { get; set; }

    public decimal CommissionPct { get; set; }

    public decimal SalesYtd { get; set; }

    public decimal SalesLastYear { get; set; }
}
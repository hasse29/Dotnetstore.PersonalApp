namespace Dotnetstore.PersonalApp.Modules.Sales.Domain;

/// <summary>sales territory.</summary>
internal sealed class SalesTerritory
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string CountryRegionCode { get; set; } = string.Empty;

    public string RegionGroup { get; set; } = string.Empty;

    public decimal SalesYtd { get; set; }

    public decimal SalesLastYear { get; set; }

    public decimal CostYtd { get; set; }

    public decimal CostLastYear { get; set; }
}
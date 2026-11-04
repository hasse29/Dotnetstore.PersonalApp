namespace Dotnetstore.PersonalApp.Modules.Sales.Domain;

/// <summary>country currency.</summary>
internal sealed class CountryRegionCurrency
{
    public Guid Id { get; set; }

    public string CountryRegion { get; set; } = string.Empty;

    public Guid CurrencyId { get; set; }

    public Currency Currency { get; set; } = null!;
}
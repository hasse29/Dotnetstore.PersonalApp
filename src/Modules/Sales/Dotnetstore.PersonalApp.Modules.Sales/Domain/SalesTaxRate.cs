namespace Dotnetstore.PersonalApp.Modules.Sales.Domain;

/// <summary>tax rate.</summary>
internal sealed class SalesTaxRate
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string StateProvince { get; set; } = string.Empty;

    public string TaxType { get; set; } = string.Empty;

    public decimal TaxRate { get; set; }
}
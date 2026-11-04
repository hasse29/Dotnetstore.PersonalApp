namespace Dotnetstore.PersonalApp.Modules.Sales.Domain;

/// <summary>customer.</summary>
internal sealed class Customer
{
    public Guid Id { get; set; }

    public string Number { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public Guid? ClientId { get; set; }

    public Guid? StoreId { get; set; }

    public Store? Store { get; set; }

    public Guid? TerritoryId { get; set; }

    public SalesTerritory? Territory { get; set; }
}
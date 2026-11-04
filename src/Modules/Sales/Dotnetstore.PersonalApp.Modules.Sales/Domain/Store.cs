namespace Dotnetstore.PersonalApp.Modules.Sales.Domain;

/// <summary>store.</summary>
internal sealed class Store
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public Guid? SalesPersonId { get; set; }

    public SalesPerson? SalesPerson { get; set; }
}
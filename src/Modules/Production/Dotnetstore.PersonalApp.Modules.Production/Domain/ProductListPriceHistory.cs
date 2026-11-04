namespace Dotnetstore.PersonalApp.Modules.Production.Domain;

/// <summary>list price history entry.</summary>
internal sealed class ProductListPriceHistory
{
    public Guid Id { get; set; }

    public Guid ProductId { get; set; }

    public Product Product { get; set; } = null!;

    public DateOnly StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public decimal ListPrice { get; set; }
}
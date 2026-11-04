namespace Dotnetstore.PersonalApp.Modules.Production.Domain;

/// <summary>cost history entry.</summary>
internal sealed class ProductCostHistory
{
    public Guid Id { get; set; }

    public Guid ProductId { get; set; }

    public Product Product { get; set; } = null!;

    public DateOnly StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public decimal StandardCost { get; set; }
}
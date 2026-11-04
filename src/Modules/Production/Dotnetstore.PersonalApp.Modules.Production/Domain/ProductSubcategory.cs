namespace Dotnetstore.PersonalApp.Modules.Production.Domain;

/// <summary>product subcategory.</summary>
internal sealed class ProductSubcategory
{
    public Guid Id { get; set; }

    public Guid CategoryId { get; set; }

    public ProductCategory Category { get; set; } = null!;

    public string Name { get; set; } = string.Empty;
}
namespace Dotnetstore.PersonalApp.Modules.Production.Domain;

/// <summary>product category.</summary>
internal sealed class ProductCategory
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;
}
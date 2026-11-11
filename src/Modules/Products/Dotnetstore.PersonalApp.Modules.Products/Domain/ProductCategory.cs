namespace Dotnetstore.PersonalApp.Modules.Products.Domain;

/// <summary>A user-managed grouping of products, e.g. Network cables or Web apps.</summary>
internal sealed class ProductCategory
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}

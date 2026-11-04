namespace Dotnetstore.PersonalApp.Modules.Production.Domain;

/// <summary>product model.</summary>
internal sealed class ProductModel
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string? Instructions { get; set; }
}
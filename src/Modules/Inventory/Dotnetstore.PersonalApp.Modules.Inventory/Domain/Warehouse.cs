namespace Dotnetstore.PersonalApp.Modules.Inventory.Domain;

/// <summary>warehouse.</summary>
internal sealed class Warehouse
{
    public Guid Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Address { get; set; }

    public bool Active { get; set; }
}
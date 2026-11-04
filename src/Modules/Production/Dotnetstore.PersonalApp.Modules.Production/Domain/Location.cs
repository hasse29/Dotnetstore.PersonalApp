namespace Dotnetstore.PersonalApp.Modules.Production.Domain;

/// <summary>production location.</summary>
internal sealed class Location
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public decimal CostRate { get; set; }

    public decimal Availability { get; set; }
}
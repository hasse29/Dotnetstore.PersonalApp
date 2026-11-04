namespace Dotnetstore.PersonalApp.Modules.Purchasing.Domain;

/// <summary>ship method.</summary>
internal sealed class ShipMethod
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public decimal ShipBase { get; set; }

    public decimal ShipRate { get; set; }
}
namespace Dotnetstore.PersonalApp.Modules.Sales.Domain;

/// <summary>currency.</summary>
internal sealed class Currency
{
    public Guid Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;
}
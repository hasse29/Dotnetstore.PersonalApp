namespace Dotnetstore.PersonalApp.Modules.Sales.Domain;

/// <summary>sales reason.</summary>
internal sealed class SalesReason
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string ReasonType { get; set; } = string.Empty;
}
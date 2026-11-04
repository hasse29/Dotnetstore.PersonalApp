namespace Dotnetstore.PersonalApp.Modules.Production.Domain;

/// <summary>unit of measure.</summary>
internal sealed class UnitMeasure
{
    public Guid Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;
}
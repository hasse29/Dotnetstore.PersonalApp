namespace Dotnetstore.PersonalApp.Modules.Production.Domain;

/// <summary>scrap reason.</summary>
internal sealed class ScrapReason
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;
}
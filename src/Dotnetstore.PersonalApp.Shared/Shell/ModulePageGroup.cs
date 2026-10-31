namespace Dotnetstore.PersonalApp.Shared.Shell;

/// <summary>
/// A collapsible navigation group. Pages from different modules share a group by <paramref name="Key"/>.
/// </summary>
/// <param name="Key">Identifies the group across modules.</param>
/// <param name="Title">Localized header text.</param>
/// <param name="IconData">SVG path data (24x24 viewbox) for the header icon.</param>
/// <param name="Order">Sorts the group among top-level entries; ungrouped pages count as 0, so a higher value places the group after them.</param>
public sealed record ModulePageGroup(string Key, string Title, string IconData, int Order = 0);
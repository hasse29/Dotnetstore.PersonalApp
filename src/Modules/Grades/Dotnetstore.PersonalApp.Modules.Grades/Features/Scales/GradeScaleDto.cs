using Dotnetstore.PersonalApp.Modules.Grades.Domain;

namespace Dotnetstore.PersonalApp.Modules.Grades.Features.Scales;

internal sealed record GradeScaleItemDto(Guid Id, string Label, int Rank, string? Description, bool IsPassing)
{
    public static GradeScaleItemDto From(GradeScaleItem item) => new(item.Id, item.Label, item.Rank, item.Description, item.IsPassing);
}

/// <summary>A scale with its steps, the best step first.</summary>
internal sealed record GradeScaleDto(Guid Id, string Name, string? Country, string? Description, IReadOnlyList<GradeScaleItemDto> Items)
{
    /// <summary>"A-F" or "A-F (Sweden)": the country tells apart scales that share a name.</summary>
    public string DisplayName => string.IsNullOrEmpty(Country) ? Name : $"{Name} ({Country})";

    /// <summary>The steps from best to worst on one line, e.g. "A, B, C, D, E, F".</summary>
    public string Summary => string.Join(", ", Items.Select(i => i.Label));

    public static GradeScaleDto From(GradeScale scale) => new(
        scale.Id,
        scale.Name,
        scale.Country,
        scale.Description,
        [.. scale.Items.OrderByDescending(i => i.Rank).ThenBy(i => i.Label, StringComparer.CurrentCultureIgnoreCase).Select(GradeScaleItemDto.From)]);
}

namespace Dotnetstore.PersonalApp.Modules.Cvs.Features.Cvs;

/// <summary>The technology tags of a job: short free-text labels such as "C#" or "PostgreSQL", stored as one comma-separated string.</summary>
internal static class TechnologyTags
{
    public const int MaxTags = 20;
    public const int MaxTagLength = 30;

    public static IReadOnlyList<string> Parse(string stored) => Normalize(stored.Split(','));

    /// <summary>Splits what the user typed (commas, semicolons or line breaks) into tags.</summary>
    public static IReadOnlyList<string> Split(string? input) =>
        string.IsNullOrWhiteSpace(input) ? [] : Normalize(input.Split([',', ';', '\n', '\r']));

    /// <summary>Trimmed, non-empty tags without duplicates (ignoring case); a tag never contains a comma.</summary>
    public static IReadOnlyList<string> Normalize(IEnumerable<string> tags) =>
        [.. tags.SelectMany(t => t.Split(','))
            .Select(t => t.Trim())
            .Where(t => t.Length > 0)
            .DistinctBy(t => t, StringComparer.OrdinalIgnoreCase)];

    public static string Join(IEnumerable<string> tags) => string.Join(',', Normalize(tags));
}

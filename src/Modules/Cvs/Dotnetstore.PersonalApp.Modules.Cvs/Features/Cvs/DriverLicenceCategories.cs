namespace Dotnetstore.PersonalApp.Modules.Cvs.Features.Cvs;

/// <summary>The EU driving licence categories a CV can list, in the order they are shown and stored.</summary>
internal static class DriverLicenceCategories
{
    public static IReadOnlyList<string> All { get; } = ["AM", "A1", "A2", "A", "B", "BE", "C1", "C1E", "C", "CE", "D1", "D1E", "D", "DE"];

    public static IReadOnlyList<string> Parse(string stored) =>
        stored.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    /// <summary>Distinct, known categories in canonical order.</summary>
    public static string Join(IEnumerable<string> categories)
    {
        var chosen = categories.ToHashSet(StringComparer.OrdinalIgnoreCase);
        return string.Join(',', All.Where(chosen.Contains));
    }
}

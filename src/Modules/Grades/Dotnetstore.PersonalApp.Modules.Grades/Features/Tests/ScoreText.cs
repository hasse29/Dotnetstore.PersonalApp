using System.Globalization;

namespace Dotnetstore.PersonalApp.Modules.Grades.Features.Tests;

/// <summary>Scores are typed into text boxes; a teacher may use either a decimal comma or a decimal point.</summary>
internal static class ScoreText
{
    /// <summary>NaN for text that is not a number, which the handlers reject as an invalid score.</summary>
    public static double Parse(string? text)
    {
        var trimmed = text?.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            return double.NaN;
        }

        if (double.TryParse(trimmed, NumberStyles.Float, CultureInfo.CurrentCulture, out var current))
        {
            return current;
        }

        return double.TryParse(trimmed.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out var invariant) ? invariant : double.NaN;
    }

    public static string Format(double score) => score.ToString("0.##", CultureInfo.CurrentCulture);
}

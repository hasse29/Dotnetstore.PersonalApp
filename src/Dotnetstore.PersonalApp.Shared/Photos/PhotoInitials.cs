namespace Dotnetstore.PersonalApp.Shared.Photos;

public static class PhotoInitials
{
    /// <summary>The first letters of the first two words, e.g. "Acme Tools AB" gives "AT". Empty for a blank name.</summary>
    public static string From(string name)
    {
        ArgumentNullException.ThrowIfNull(name);

        return string.Concat(
            name.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Take(2)
                .Select(part => char.ToUpperInvariant(part[0])));
    }
}

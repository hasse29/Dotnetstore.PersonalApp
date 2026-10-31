using System.Globalization;

namespace Dotnetstore.PersonalApp.Shared.Localization;

public interface ILocalizationService
{
    CultureInfo Culture { get; }

    IReadOnlyList<LanguageOption> Languages { get; }

    /// <summary>Raised after <see cref="SetCulture"/> changed the language. Screens are rebuilt in response.</summary>
    event EventHandler? CultureChanged;

    /// <summary>The translation for <paramref name="key"/>, or the key itself when no resource defines it.</summary>
    string Get(string key);

    /// <summary>The translation for <paramref name="key"/>, or null when no resource defines it.</summary>
    string? Find(string key);

    string Format(string key, params object[] args);

    /// <summary>Switches language. Unsupported cultures fall back to the default (English).</summary>
    void SetCulture(string cultureName);
}

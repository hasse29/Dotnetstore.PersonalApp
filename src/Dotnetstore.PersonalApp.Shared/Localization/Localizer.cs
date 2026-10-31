using System.Globalization;
using System.Resources;
using Dotnetstore.PersonalApp.Shared.Resources;

namespace Dotnetstore.PersonalApp.Shared.Localization;

/// <summary>
/// Looks keys up in the resource files that the host and each module register. XAML reaches it through
/// <see cref="Instance"/> (via <see cref="TrExtension"/>); everything else gets it from DI as <see cref="ILocalizationService"/>.
/// </summary>
public sealed class Localizer : ILocalizationService
{
    public const string DefaultCulture = "en";

    private readonly List<ResourceManager> _resources = [];

    public Localizer()
    {
        AddResources(SharedStrings.ResourceManager);
    }

    public static Localizer Instance { get; } = new();

    public CultureInfo Culture { get; private set; } = CultureInfo.GetCultureInfo(DefaultCulture);

    public IReadOnlyList<LanguageOption> Languages { get; } =
    [
        new("en", "English"),
        new("sv", "Svenska"),
        new("zh-CN", "简体中文"),
    ];

    public event EventHandler? CultureChanged;

    public void AddResources(ResourceManager resources)
    {
        if (!_resources.Contains(resources))
        {
            _resources.Add(resources);
        }
    }

    public string? Find(string key)
    {
        foreach (var resources in _resources)
        {
            if (resources.GetString(key, Culture) is { } value)
            {
                return value;
            }
        }

        return null;
    }

    public string Get(string key) => Find(key) ?? key;

    public string Format(string key, params object[] args) => string.Format(Culture, Get(key), args);

    public void SetCulture(string cultureName)
    {
        var option = Languages.FirstOrDefault(l => string.Equals(l.Culture, cultureName, StringComparison.OrdinalIgnoreCase))
            ?? Languages[0];
        var culture = CultureInfo.GetCultureInfo(option.Culture);

        if (culture.Equals(Culture))
        {
            return;
        }

        Culture = culture;
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;

        CultureChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>The supported culture that best matches <paramref name="culture"/> (by language), or English.</summary>
    public string Match(CultureInfo culture) =>
        Languages.FirstOrDefault(l => CultureInfo.GetCultureInfo(l.Culture).TwoLetterISOLanguageName == culture.TwoLetterISOLanguageName)?.Culture
        ?? DefaultCulture;
}

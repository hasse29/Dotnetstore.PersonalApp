using System.Collections;
using System.Globalization;
using Dotnetstore.PersonalApp.Host.Resources;

namespace Dotnetstore.PersonalApp.Integration.Tests;

public sealed class HostLocalizationTests
{
    private static HashSet<string> Keys(string culture)
    {
        var set = HostStrings.ResourceManager.GetResourceSet(culture == "en" ? CultureInfo.InvariantCulture : CultureInfo.GetCultureInfo(culture), createIfNotExists: true, tryParents: false);
        return set is null ? [] : [.. set.Cast<DictionaryEntry>().Select(e => (string)e.Key)];
    }

    [Theory]
    [InlineData("sv")]
    [InlineData("zh-Hans")]
    public void EveryEnglishKeyIsTranslated(string culture)
    {
        var english = Keys("en");

        english.ShouldNotBeEmpty();
        english.Except(Keys(culture)).ShouldBeEmpty();
        Keys(culture).Except(english).ShouldBeEmpty();
    }
}

public sealed class SharedLocalizationTests
{
    private static HashSet<string> Keys(string culture)
    {
        var set = Dotnetstore.PersonalApp.Shared.Resources.SharedStrings.ResourceManager.GetResourceSet(
            culture == "en" ? CultureInfo.InvariantCulture : CultureInfo.GetCultureInfo(culture),
            createIfNotExists: true,
            tryParents: false);
        return set is null ? [] : [.. set.Cast<DictionaryEntry>().Select(e => (string)e.Key)];
    }

    [Theory]
    [InlineData("sv")]
    [InlineData("zh-Hans")]
    public void EveryEnglishKeyIsTranslated(string culture)
    {
        var english = Keys("en");

        english.ShouldNotBeEmpty();
        english.Except(Keys(culture)).ShouldBeEmpty();
        Keys(culture).Except(english).ShouldBeEmpty();
    }
}

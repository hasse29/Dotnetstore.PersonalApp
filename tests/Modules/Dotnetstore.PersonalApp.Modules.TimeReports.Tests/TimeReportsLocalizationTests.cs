using System.Collections;
using System.Globalization;
using Dotnetstore.PersonalApp.Modules.TimeReports.Resources;

namespace Dotnetstore.PersonalApp.Modules.TimeReports.Tests;

public sealed class TimeReportsLocalizationTests
{
    private static HashSet<string> Keys(string culture)
    {
        var set = TimeReportsStrings.ResourceManager.GetResourceSet(
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

        Assert.NotEmpty(english);
        Assert.Empty(english.Except(Keys(culture)));
        Assert.Empty(Keys(culture).Except(english));
    }
}
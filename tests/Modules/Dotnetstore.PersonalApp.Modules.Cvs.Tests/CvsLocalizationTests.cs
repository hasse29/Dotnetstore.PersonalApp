using System.Collections;
using System.Globalization;
using Dotnetstore.PersonalApp.Modules.Cvs.Resources;

namespace Dotnetstore.PersonalApp.Modules.Cvs.Tests;

public sealed class CvsLocalizationTests
{
    private static HashSet<string> Keys(string culture)
    {
        var set = CvsStrings.ResourceManager.GetResourceSet(
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

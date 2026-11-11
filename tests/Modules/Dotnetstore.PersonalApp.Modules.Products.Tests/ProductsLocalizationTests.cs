using System.Collections;
using System.Globalization;
using Dotnetstore.PersonalApp.Modules.Products.Resources;

namespace Dotnetstore.PersonalApp.Modules.Products.Tests;

public sealed class ProductsLocalizationTests
{
    private static HashSet<string> Keys(string culture)
    {
        var set = ProductsStrings.ResourceManager.GetResourceSet(
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

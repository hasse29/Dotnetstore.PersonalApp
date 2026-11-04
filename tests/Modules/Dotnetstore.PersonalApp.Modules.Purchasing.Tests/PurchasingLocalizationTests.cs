using System.Collections;
using System.Globalization;
using Dotnetstore.PersonalApp.Modules.Purchasing.Resources;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Results;

namespace Dotnetstore.PersonalApp.Modules.Purchasing.Tests;

public sealed class PurchasingLocalizationTests
{
    private static HashSet<string> Keys(string culture)
    {
        var set = PurchasingStrings.ResourceManager.GetResourceSet(
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

    [Theory]
    [InlineData("en", "The record was not found.")]
    [InlineData("sv", "Posten hittades inte.")]
    [InlineData("zh-CN", "未找到该记录。")]
    public void DescribesErrorsInTheCurrentLanguage(string culture, string expected)
    {
        var localizer = new Localizer();
        localizer.AddResources(PurchasingStrings.ResourceManager);
        localizer.SetCulture(culture);

        localizer.Describe(Error.NotFound("ship_method_not_found", "The record was not found.")).ShouldBe(expected);
    }
}
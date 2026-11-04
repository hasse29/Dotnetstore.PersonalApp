using System.Collections;
using System.Globalization;
using Dotnetstore.PersonalApp.Modules.Sales.Resources;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Results;

namespace Dotnetstore.PersonalApp.Modules.Sales.Tests;

public sealed class SalesLocalizationTests
{
    private static HashSet<string> Keys(string culture)
    {
        var set = SalesStrings.ResourceManager.GetResourceSet(
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
        localizer.AddResources(SalesStrings.ResourceManager);
        localizer.SetCulture(culture);

        localizer.Describe(Error.NotFound("sales_territory_not_found", "The record was not found.")).ShouldBe(expected);
    }
}
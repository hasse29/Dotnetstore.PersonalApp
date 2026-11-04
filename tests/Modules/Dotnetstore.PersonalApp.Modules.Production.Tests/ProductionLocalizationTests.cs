using System.Collections;
using System.Globalization;
using Dotnetstore.PersonalApp.Modules.Production.Resources;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Results;

namespace Dotnetstore.PersonalApp.Modules.Production.Tests;

public sealed class ProductionLocalizationTests
{
    private static HashSet<string> Keys(string culture)
    {
        var set = ProductionStrings.ResourceManager.GetResourceSet(
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
        localizer.AddResources(ProductionStrings.ResourceManager);
        localizer.SetCulture(culture);

        localizer.Describe(Error.NotFound("unit_measure_not_found", "The record was not found.")).ShouldBe(expected);
    }
}
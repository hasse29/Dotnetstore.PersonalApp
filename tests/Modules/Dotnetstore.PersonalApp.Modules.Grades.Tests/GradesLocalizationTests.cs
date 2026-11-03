using System.Collections;
using System.Globalization;
using Dotnetstore.PersonalApp.Modules.Grades.Resources;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Results;

namespace Dotnetstore.PersonalApp.Modules.Grades.Tests;

public sealed class GradesLocalizationTests
{
    private static HashSet<string> Keys(string culture)
    {
        var set = GradesStrings.ResourceManager.GetResourceSet(
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
    [InlineData("en", "Grade scale not found.")]
    [InlineData("sv", "Betygsskalan hittades inte.")]
    [InlineData("zh-CN", "未找到评分标准。")]
    public void DescribesGradeErrorsInTheCurrentLanguage(string culture, string expected)
    {
        var localizer = new Localizer();
        localizer.AddResources(GradesStrings.ResourceManager);
        localizer.SetCulture(culture);

        localizer.Describe(Error.NotFound("grade_scale_not_found", "Grade scale not found.")).ShouldBe(expected);
    }
}

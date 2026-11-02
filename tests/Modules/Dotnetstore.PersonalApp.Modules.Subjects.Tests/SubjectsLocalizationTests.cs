using System.Collections;
using System.Globalization;
using Dotnetstore.PersonalApp.Modules.Subjects.Resources;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Results;

namespace Dotnetstore.PersonalApp.Modules.Subjects.Tests;

public sealed class SubjectsLocalizationTests
{
    private static HashSet<string> Keys(string culture)
    {
        var set = SubjectsStrings.ResourceManager.GetResourceSet(
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
    [InlineData("en", "Subject not found.")]
    [InlineData("sv", "Ämnet hittades inte.")]
    [InlineData("zh-CN", "未找到该科目。")]
    public void DescribesSubjectErrorsInTheCurrentLanguage(string culture, string expected)
    {
        var localizer = new Localizer();
        localizer.AddResources(SubjectsStrings.ResourceManager);
        localizer.SetCulture(culture);

        localizer.Describe(Error.NotFound("subject_not_found", "Subject not found.")).ShouldBe(expected);
    }

    [Fact]
    public void CancelComesFromTheSharedStringsSoModulesDoNotDependOnEachOther()
    {
        var localizer = new Localizer();
        localizer.AddResources(SubjectsStrings.ResourceManager);
        localizer.SetCulture("sv");

        localizer.Get("Common_Cancel").ShouldBe("Avbryt");
    }
}



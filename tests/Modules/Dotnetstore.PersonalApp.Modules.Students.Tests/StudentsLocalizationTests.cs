using System.Collections;
using System.Globalization;
using Dotnetstore.PersonalApp.Modules.Students.Resources;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Results;

namespace Dotnetstore.PersonalApp.Modules.Students.Tests;

public sealed class StudentsLocalizationTests
{
    private static HashSet<string> Keys(string culture)
    {
        var set = StudentsStrings.ResourceManager.GetResourceSet(
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
    [InlineData("en", "Student not found.")]
    [InlineData("sv", "Eleven hittades inte.")]
    [InlineData("zh-CN", "未找到该学生。")]
    public void DescribesStudentErrorsInTheCurrentLanguage(string culture, string expected)
    {
        var localizer = new Localizer();
        localizer.AddResources(StudentsStrings.ResourceManager);
        localizer.SetCulture(culture);

        localizer.Describe(Error.NotFound("student_not_found", "Student not found.")).ShouldBe(expected);
    }

    [Fact]
    public void CancelComesFromTheSharedStringsSoModulesDoNotDependOnEachOther()
    {
        var localizer = new Localizer();
        localizer.AddResources(StudentsStrings.ResourceManager);
        localizer.SetCulture("sv");

        localizer.Get("Common_Cancel").ShouldBe("Avbryt");
    }
}



using System.Collections;
using System.Globalization;
using Dotnetstore.PersonalApp.Modules.Schedule.Features.Entries;
using Dotnetstore.PersonalApp.Modules.Schedule.Resources;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Results;

namespace Dotnetstore.PersonalApp.Modules.Schedule.Tests;

public sealed class ScheduleLocalizationTests
{
    private static HashSet<string> Keys(string culture)
    {
        var set = ScheduleStrings.ResourceManager.GetResourceSet(
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
    [InlineData("en", "You already have a lesson at that time.")]
    [InlineData("sv", "Du har redan en lektion vid den tiden.")]
    [InlineData("zh-CN", "您在该时间已有课程。")]
    public void DescribesScheduleErrorsInTheCurrentLanguage(string culture, string expected)
    {
        var localizer = new Localizer();
        localizer.AddResources(ScheduleStrings.ResourceManager);
        localizer.SetCulture(culture);

        localizer.Describe(Error.Conflict("schedule_overlap", "overlap")).ShouldBe(expected);
    }

    [Fact]
    public void WeekdaysAreNamedInTheCurrentLanguageStartingOnMonday()
    {
        var localizer = new Localizer();
        localizer.SetCulture("sv");

        var days = DayOption.All(localizer);

        days.Select(d => d.Name).ShouldBe(["Måndag", "Tisdag", "Onsdag", "Torsdag", "Fredag", "Lördag", "Söndag"]);
    }
}

using System.Collections;
using System.Globalization;
using Dotnetstore.PersonalApp.Modules.PhoneNumbers.Resources;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Results;

namespace Dotnetstore.PersonalApp.Modules.PhoneNumbers.Tests;

public sealed class PhoneNumbersLocalizationTests
{
    private static HashSet<string> Keys(string culture)
    {
        var set = PhoneNumbersStrings.ResourceManager.GetResourceSet(
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
    [InlineData("en", "Phone number type not found.")]
    [InlineData("sv", "Telefonnummertypen hittades inte.")]
    [InlineData("zh-CN", "未找到该电话号码类型。")]
    public void DescribesPhoneNumberTypeErrorsInTheCurrentLanguage(string culture, string expected)
    {
        var localizer = new Localizer();
        localizer.AddResources(PhoneNumbersStrings.ResourceManager);
        localizer.SetCulture(culture);

        localizer.Describe(Error.NotFound("phone_number_type_not_found", "Phone number type not found.")).ShouldBe(expected);
    }
}

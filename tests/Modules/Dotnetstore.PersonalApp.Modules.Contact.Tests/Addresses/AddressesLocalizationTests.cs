using Dotnetstore.PersonalApp.Modules.Contact.Addresses;
using Dotnetstore.PersonalApp.Modules.Contact.Resources;
using System.Collections;
using System.Globalization;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Results;

namespace Dotnetstore.PersonalApp.Modules.Contact.Tests.Addresses;

public sealed class AddressesLocalizationTests
{
    private static HashSet<string> Keys(string culture)
    {
        var set = ContactStrings.ResourceManager.GetResourceSet(
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
    [InlineData("en", "Address type not found.")]
    [InlineData("sv", "Adresstypen hittades inte.")]
    [InlineData("zh-CN", "未找到该地址类型。")]
    public void DescribesAddressTypeErrorsInTheCurrentLanguage(string culture, string expected)
    {
        var localizer = new Localizer();
        localizer.AddResources(ContactStrings.ResourceManager);
        localizer.SetCulture(culture);

        localizer.Describe(Error.NotFound("address_type_not_found", "Address type not found.")).ShouldBe(expected);
    }
}

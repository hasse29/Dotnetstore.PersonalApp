using System.Collections;
using System.Globalization;
using Dotnetstore.PersonalApp.Modules.Addresses.Resources;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Results;

namespace Dotnetstore.PersonalApp.Modules.Addresses.Tests;

public sealed class AddressesLocalizationTests
{
    private static HashSet<string> Keys(string culture)
    {
        var set = AddressesStrings.ResourceManager.GetResourceSet(
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

        Assert.NotEmpty(english);
        Assert.Empty(english.Except(Keys(culture)));
        Assert.Empty(Keys(culture).Except(english));
    }

    [Theory]
    [InlineData("en", "Address type not found.")]
    [InlineData("sv", "Adresstypen hittades inte.")]
    [InlineData("zh-CN", "未找到该地址类型。")]
    public void DescribesAddressTypeErrorsInTheCurrentLanguage(string culture, string expected)
    {
        var localizer = new Localizer();
        localizer.AddResources(AddressesStrings.ResourceManager);
        localizer.SetCulture(culture);

        Assert.Equal(expected, localizer.Describe(Error.NotFound("address_type_not_found", "Address type not found.")));
    }
}

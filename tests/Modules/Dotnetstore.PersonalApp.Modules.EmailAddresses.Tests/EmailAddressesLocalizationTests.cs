using System.Collections;
using System.Globalization;
using Dotnetstore.PersonalApp.Modules.EmailAddresses.Resources;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Results;

namespace Dotnetstore.PersonalApp.Modules.EmailAddresses.Tests;

public sealed class EmailAddressesLocalizationTests
{
    private static HashSet<string> Keys(string culture)
    {
        var set = EmailAddressesStrings.ResourceManager.GetResourceSet(
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
    [InlineData("en", "Email address type not found.")]
    [InlineData("sv", "E-postadresstypen hittades inte.")]
    [InlineData("zh-CN", "未找到该电子邮件地址类型。")]
    public void DescribesEmailAddressTypeErrorsInTheCurrentLanguage(string culture, string expected)
    {
        var localizer = new Localizer();
        localizer.AddResources(EmailAddressesStrings.ResourceManager);
        localizer.SetCulture(culture);

        Assert.Equal(expected, localizer.Describe(Error.NotFound("email_address_type_not_found", "Email address type not found.")));
    }
}

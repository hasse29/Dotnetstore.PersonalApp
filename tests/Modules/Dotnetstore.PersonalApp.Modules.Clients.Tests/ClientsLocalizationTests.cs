using System.Collections;
using System.Globalization;
using Dotnetstore.PersonalApp.Modules.Clients.Resources;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Results;

namespace Dotnetstore.PersonalApp.Modules.Clients.Tests;

public sealed class ClientsLocalizationTests
{
    private static HashSet<string> Keys(string culture)
    {
        var set = ClientsStrings.ResourceManager.GetResourceSet(
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
    [InlineData("en", "Client not found.")]
    [InlineData("sv", "Uppdragsgivaren hittades inte.")]
    [InlineData("zh-CN", "未找到该委托方。")]
    public void DescribesClientErrorsInTheCurrentLanguage(string culture, string expected)
    {
        var localizer = new Localizer();
        localizer.AddResources(ClientsStrings.ResourceManager);
        localizer.SetCulture(culture);

        localizer.Describe(Error.NotFound("client_not_found", "Client not found.")).ShouldBe(expected);
    }

    [Fact]
    public void CancelComesFromTheSharedStringsSoModulesDoNotDependOnEachOther()
    {
        var localizer = new Localizer();
        localizer.AddResources(ClientsStrings.ResourceManager);
        localizer.SetCulture("sv");

        localizer.Get("Common_Cancel").ShouldBe("Avbryt");
    }
}



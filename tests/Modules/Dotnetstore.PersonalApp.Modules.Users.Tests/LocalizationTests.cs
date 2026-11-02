using System.Globalization;
using System.Resources;
using Dotnetstore.PersonalApp.Modules.Users.Resources;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Results;

namespace Dotnetstore.PersonalApp.Modules.Users.Tests;

public sealed class LocalizationTests
{
    private static Localizer CreateLocalizer(string culture)
    {
        var localizer = new Localizer();
        localizer.AddResources(UsersStrings.ResourceManager);
        localizer.SetCulture(culture);
        return localizer;
    }

    private static HashSet<string> Keys(ResourceManager manager, string culture)
    {
        var set = manager.GetResourceSet(culture == "en" ? CultureInfo.InvariantCulture : CultureInfo.GetCultureInfo(culture), createIfNotExists: true, tryParents: false);
        return set is null ? [] : [.. set.Cast<System.Collections.DictionaryEntry>().Select(e => (string)e.Key)];
    }

    [Theory]
    [InlineData("sv")]
    [InlineData("zh-Hans")]
    public void EveryEnglishKeyIsTranslated(string culture)
    {
        var english = Keys(UsersStrings.ResourceManager, "en");
        var translated = Keys(UsersStrings.ResourceManager, culture);

        english.ShouldNotBeEmpty();
        english.Except(translated).ShouldBeEmpty();
        translated.Except(english).ShouldBeEmpty();
    }

    [Theory]
    [InlineData("en", "Sign in")]
    [InlineData("sv", "Logga in")]
    [InlineData("zh-CN", "登录")]
    public void LooksUpTextInTheCurrentLanguage(string culture, string expected)
    {
        CreateLocalizer(culture).Get("Login_SignIn").ShouldBe(expected);
    }

    [Fact]
    public void FormatsWithTheCurrentCulture()
    {
        CreateLocalizer("sv").Format("Users_Count", 4).ShouldBe("4 användare");
    }

    [Fact]
    public void UnknownKeysFallBackToTheKeyAndUnsupportedCulturesToEnglish()
    {
        var localizer = CreateLocalizer("fr");

        localizer.Culture.Name.ShouldBe("en");
        localizer.Get("No_Such_Key").ShouldBe("No_Such_Key");
        localizer.Find("No_Such_Key").ShouldBeNull();
    }

    [Fact]
    public void ChangingCultureRaisesAnEventOnlyWhenItChanges()
    {
        var localizer = CreateLocalizer("en");
        var raised = 0;
        localizer.CultureChanged += (_, _) => raised++;

        localizer.SetCulture("en");
        localizer.SetCulture("sv");
        localizer.SetCulture("sv");

        raised.ShouldBe(1);
    }

    [Theory]
    [InlineData("en", "Invalid username or password.")]
    [InlineData("sv", "Fel användarnamn eller lösenord.")]
    [InlineData("zh-CN", "用户名或密码错误。")]
    public void DescribesErrorsByCode(string culture, string expected)
    {
        CreateLocalizer(culture).Describe(Error.InvalidCredentials()).ShouldBe(expected);
    }

    [Fact]
    public void DescribeFallsBackToTheErrorMessageForUnknownCodes()
    {
        CreateLocalizer("sv").Describe(new Error("odd_code", "Something odd")).ShouldBe("Something odd");
    }

    [Theory]
    [InlineData("en-US", "en")]
    [InlineData("sv-SE", "sv")]
    [InlineData("zh-Hans-CN", "zh-CN")]
    [InlineData("de-DE", "en")]
    public void MatchesTheSystemLanguageToASupportedOne(string system, string expected)
    {
        (new Localizer().Match(CultureInfo.GetCultureInfo(system))).ShouldBe(expected);
    }
}

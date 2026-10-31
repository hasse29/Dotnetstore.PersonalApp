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

        Assert.NotEmpty(english);
        Assert.Empty(english.Except(translated));
        Assert.Empty(translated.Except(english));
    }

    [Theory]
    [InlineData("en", "Sign in")]
    [InlineData("sv", "Logga in")]
    [InlineData("zh-CN", "登录")]
    public void LooksUpTextInTheCurrentLanguage(string culture, string expected)
    {
        Assert.Equal(expected, CreateLocalizer(culture).Get("Login_SignIn"));
    }

    [Fact]
    public void FormatsWithTheCurrentCulture()
    {
        Assert.Equal("4 användare", CreateLocalizer("sv").Format("Users_Count", 4));
    }

    [Fact]
    public void UnknownKeysFallBackToTheKeyAndUnsupportedCulturesToEnglish()
    {
        var localizer = CreateLocalizer("fr");

        Assert.Equal("en", localizer.Culture.Name);
        Assert.Equal("No_Such_Key", localizer.Get("No_Such_Key"));
        Assert.Null(localizer.Find("No_Such_Key"));
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

        Assert.Equal(1, raised);
    }

    [Theory]
    [InlineData("en", "Invalid username or password.")]
    [InlineData("sv", "Fel användarnamn eller lösenord.")]
    [InlineData("zh-CN", "用户名或密码错误。")]
    public void DescribesErrorsByCode(string culture, string expected)
    {
        Assert.Equal(expected, CreateLocalizer(culture).Describe(Error.InvalidCredentials()));
    }

    [Fact]
    public void DescribeFallsBackToTheErrorMessageForUnknownCodes()
    {
        Assert.Equal("Something odd", CreateLocalizer("sv").Describe(new Error("odd_code", "Something odd")));
    }

    [Theory]
    [InlineData("en-US", "en")]
    [InlineData("sv-SE", "sv")]
    [InlineData("zh-Hans-CN", "zh-CN")]
    [InlineData("de-DE", "en")]
    public void MatchesTheSystemLanguageToASupportedOne(string system, string expected)
    {
        Assert.Equal(expected, new Localizer().Match(CultureInfo.GetCultureInfo(system)));
    }
}

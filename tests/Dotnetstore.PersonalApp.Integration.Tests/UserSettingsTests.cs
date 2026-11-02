using Dotnetstore.PersonalApp.Host;

namespace Dotnetstore.PersonalApp.Integration.Tests;

public sealed class UserSettingsTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "personalapp-settings-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    [Fact]
    public void LoadsDefaultsWhenNothingHasBeenSaved()
    {
        UserSettings.Load(_directory).Culture.ShouldBeNull();
    }

    [Fact]
    public void SavedCultureIsLoadedBack()
    {
        new UserSettings { Culture = "sv" }.Save(_directory);

        UserSettings.Load(_directory).Culture.ShouldBe("sv");
    }

    [Fact]
    public void CorruptSettingsFallBackToDefaults()
    {
        Directory.CreateDirectory(_directory);
        File.WriteAllText(Path.Combine(_directory, "settings.json"), "{ not json");

        UserSettings.Load(_directory).Culture.ShouldBeNull();
    }
}

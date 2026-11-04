using System.Text.Json;

namespace Dotnetstore.PersonalApp.Host;

/// <summary>Per-machine preferences stored next to the databases (currently just the chosen language).</summary>
internal sealed class UserSettings
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public string? Culture { get; set; }

    /// <summary>Whether the app asks GitHub at startup if a newer version has been released. Off if set to false in settings.json.</summary>
    public bool CheckForUpdates { get; set; } = true;

    /// <summary>Keys of modules whose menu pages are hidden.</summary>
    public List<string> DisabledModules { get; set; } = [];

    private static string FilePath(string directory) => Path.Combine(directory, "settings.json");

    public static UserSettings Load(string directory)
    {
        try
        {
            var path = FilePath(directory);
            return File.Exists(path)
                ? JsonSerializer.Deserialize<UserSettings>(File.ReadAllText(path)) ?? new UserSettings()
                : new UserSettings();
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            return new UserSettings();
        }
    }

    public void Save(string directory)
    {
        try
        {
            Directory.CreateDirectory(directory);
            File.WriteAllText(FilePath(directory), JsonSerializer.Serialize(this, JsonOptions));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Losing a language preference is not worth interrupting the user for.
        }
    }
}

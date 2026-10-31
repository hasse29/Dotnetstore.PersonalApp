namespace Dotnetstore.PersonalApp.Host;

internal static class AppPaths
{
    public static string DataDirectory { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Dotnetstore.PersonalApp");
}

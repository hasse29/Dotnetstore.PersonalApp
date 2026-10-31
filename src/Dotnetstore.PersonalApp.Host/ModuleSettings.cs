namespace Dotnetstore.PersonalApp.Host;

/// <summary>Which modules the user has switched off; persisted in <c>settings.json</c> and observed by the shell.</summary>
internal sealed class ModuleSettings(string directory)
{
    private readonly HashSet<string> _disabled = new(UserSettings.Load(directory).DisabledModules, StringComparer.Ordinal);

    public event EventHandler? Changed;

    public bool IsEnabled(string moduleKey) => !_disabled.Contains(moduleKey);

    public void SetEnabled(string moduleKey, bool enabled)
    {
        if (enabled ? !_disabled.Remove(moduleKey) : !_disabled.Add(moduleKey))
        {
            return;
        }

        var settings = UserSettings.Load(directory);
        settings.DisabledModules = [.. _disabled.Order(StringComparer.Ordinal)];
        settings.Save(directory);
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
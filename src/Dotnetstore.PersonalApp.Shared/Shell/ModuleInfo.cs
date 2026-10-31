namespace Dotnetstore.PersonalApp.Shared.Shell;

/// <summary>Describes a module so the shell can list it and let the user enable or disable its menu pages.</summary>
/// <param name="Key">Matches <see cref="IModulePage.ModuleKey"/> of the module's pages.</param>
/// <param name="TitleKey">Resource key of the module's display name.</param>
/// <param name="CanDisable">False for modules the app cannot work without.</param>
public sealed record ModuleInfo(string Key, string TitleKey, bool CanDisable = true);
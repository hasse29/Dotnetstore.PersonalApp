using CommunityToolkit.Mvvm.ComponentModel;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Mvvm;
using Dotnetstore.PersonalApp.Shared.Shell;

namespace Dotnetstore.PersonalApp.Host.ViewModels;

/// <summary>Settings page for switching modules on and off.</summary>
internal sealed class ModulesViewModel : ViewModelBase
{
    public ModulesViewModel(IEnumerable<ModuleInfo> modules, ModuleSettings settings, ILocalizationService localization)
    {
        Items = [.. modules.Select(module => new ModuleItemViewModel(module, localization.Get(module.TitleKey), settings))];
    }

    public IReadOnlyList<ModuleItemViewModel> Items { get; }
}

internal sealed partial class ModuleItemViewModel : ObservableObject
{
    private readonly ModuleInfo _module;
    private readonly ModuleSettings _settings;

    public ModuleItemViewModel(ModuleInfo module, string title, ModuleSettings settings)
    {
        _module = module;
        _settings = settings;
        Title = title;
        IsEnabled = settings.IsEnabled(module.Key);
    }

    public string Title { get; }

    public bool CanDisable => _module.CanDisable;

    [ObservableProperty]
    public partial bool IsEnabled { get; set; }

    partial void OnIsEnabledChanged(bool value) => _settings.SetEnabled(_module.Key, value);
}
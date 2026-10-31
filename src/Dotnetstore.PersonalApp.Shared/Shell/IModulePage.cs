using Dotnetstore.PersonalApp.Shared.Mvvm;

namespace Dotnetstore.PersonalApp.Shared.Shell;

/// <summary>
/// A top-level page a module contributes to the shell navigation. The host never references the module's view models directly.
/// </summary>
public interface IModulePage
{
    string Title { get; }

    /// <summary>SVG path data (24x24 viewbox) for the navigation icon.</summary>
    string IconData { get; }

    /// <summary>The module that owns the page; its pages are hidden while the module is disabled. Null pages are always shown.</summary>
    string? ModuleKey => null;

    /// <summary>When set, the page is shown as a sub menu entry under a collapsible group in the navigation.</summary>
    ModulePageGroup? Group => null;

    ViewModelBase CreateViewModel();
}

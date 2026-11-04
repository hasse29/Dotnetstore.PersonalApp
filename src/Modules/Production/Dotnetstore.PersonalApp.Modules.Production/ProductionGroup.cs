using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Shell;

namespace Dotnetstore.PersonalApp.Modules.Production;

/// <summary>The "Production" navigation group that all pages of the module share.</summary>
internal static class ProductionGroup
{
    public const string ModuleKey = "production";

    public static ModulePageGroup Create() => new(ModuleKey, Localizer.Instance.Get("Production_Title"), "M12 2l-5.5 9h11L12 2zm5.5 11c-2.49 0-4.5 2.01-4.5 4.5s2.01 4.5 4.5 4.5 4.5-2.01 4.5-4.5-2.01-4.5-4.5-4.5zM3 21.5h8v-8H3v8z", 1);
}
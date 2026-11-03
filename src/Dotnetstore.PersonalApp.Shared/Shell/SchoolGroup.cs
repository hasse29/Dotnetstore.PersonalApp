using Dotnetstore.PersonalApp.Shared.Localization;

namespace Dotnetstore.PersonalApp.Shared.Shell;

/// <summary>The "School" navigation group that the pages about a teacher's schools (students, subjects, schedule, grades, tests) share. They are also one module, switched on and off together under <see cref="ModuleKey"/>.</summary>
public static class SchoolGroup
{
    /// <summary>The module key of every School page, and of the single "School" entry in the module list.</summary>
    public const string ModuleKey = "school";

    // Material "school" icon
    private const string IconData = "M5 13.18v4L12 21l7-3.82v-4L12 17l-7-3.82zM12 3L1 9l11 6 9-5.18V17h2V9L12 3z";

    public static ModulePageGroup Create() => new("school", Localizer.Instance.Get("Shell_School"), IconData);
}

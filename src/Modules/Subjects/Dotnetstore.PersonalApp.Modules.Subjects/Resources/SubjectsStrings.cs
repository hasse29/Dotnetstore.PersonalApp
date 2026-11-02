using System.Resources;

namespace Dotnetstore.PersonalApp.Modules.Subjects.Resources;

internal static class SubjectsStrings
{
    public static ResourceManager ResourceManager { get; } =
        new("Dotnetstore.PersonalApp.Modules.Subjects.Resources.SubjectsStrings", typeof(SubjectsStrings).Assembly);
}

using System.Resources;

namespace Dotnetstore.PersonalApp.Modules.Grades.Resources;

internal static class GradesStrings
{
    public static ResourceManager ResourceManager { get; } =
        new("Dotnetstore.PersonalApp.Modules.Grades.Resources.GradesStrings", typeof(GradesStrings).Assembly);
}

using System.Resources;

namespace Dotnetstore.PersonalApp.Modules.Students.Resources;

internal static class StudentsStrings
{
    public static ResourceManager ResourceManager { get; } =
        new("Dotnetstore.PersonalApp.Modules.Students.Resources.StudentsStrings", typeof(StudentsStrings).Assembly);
}

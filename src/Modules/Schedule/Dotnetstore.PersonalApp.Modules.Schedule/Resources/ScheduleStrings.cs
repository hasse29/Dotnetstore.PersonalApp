using System.Resources;

namespace Dotnetstore.PersonalApp.Modules.Schedule.Resources;

internal static class ScheduleStrings
{
    public static ResourceManager ResourceManager { get; } =
        new("Dotnetstore.PersonalApp.Modules.Schedule.Resources.ScheduleStrings", typeof(ScheduleStrings).Assembly);
}

using System.Resources;

namespace Dotnetstore.PersonalApp.Modules.TimeReports.Resources;

internal static class TimeReportsStrings
{
    public static ResourceManager ResourceManager { get; } =
        new("Dotnetstore.PersonalApp.Modules.TimeReports.Resources.TimeReportsStrings", typeof(TimeReportsStrings).Assembly);
}
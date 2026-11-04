using Dotnetstore.PersonalApp.Modules.Schedule.Persistence;
using Dotnetstore.PersonalApp.Shared.Clients;
using Dotnetstore.PersonalApp.Shared.Dashboard;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Shell;
using Dotnetstore.PersonalApp.Shared.Subjects;
using Microsoft.EntityFrameworkCore;

namespace Dotnetstore.PersonalApp.Modules.Schedule.Dashboard;

/// <summary>The signed-in teacher's lessons today and how many there are in a week.</summary>
internal sealed class ScheduleDashboardWidget(
    IDbContextFactory<ScheduleDbContext> dbFactory,
    ISubjectLookup subjects,
    IClientLookup clients,
    ILocalizationService localization) : IDashboardWidget
{
    public string ModuleKey => SchoolGroup.ModuleKey;

    public int Order => 0;

    public async Task<DashboardPanel> LoadAsync(DashboardContext context, CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        var mine = await db.ScheduleEntries.AsNoTracking()
            .Where(x => x.UserId == context.UserId)
            .Select(x => new { x.Day, x.StartTime, x.EndTime, x.SubjectId, x.ClientId, x.Room })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var subjectNames = (await subjects.ListAsync(cancellationToken).ConfigureAwait(false)).ToDictionary(x => x.Id, x => x.Name);
        var clientNames = (await clients.ListAsync(cancellationToken).ConfigureAwait(false)).ToDictionary(x => x.Id, x => x.Name);

        var today = mine.Where(x => x.Day == context.Today.DayOfWeek).OrderBy(x => x.StartTime).ToList();
        var culture = localization.Culture;

        return new DashboardPanel(
            localization.Get("ScheduleDashboard_Title"),
            [
                new(localization.Get("ScheduleDashboard_Today"), today.Count.ToString(culture)),
                new(localization.Get("ScheduleDashboard_Week"), mine.Count.ToString(culture)),
            ],
            localization.Get("ScheduleDashboard_ListTitle"),
            [.. today.Select(x => new DashboardRow(
                subjectNames.GetValueOrDefault(x.SubjectId, string.Empty),
                string.Join(", ", new[] { clientNames.GetValueOrDefault(x.ClientId), x.Room }.Where(part => !string.IsNullOrWhiteSpace(part))),
                $"{x.StartTime.ToString("t", culture)}–{x.EndTime.ToString("t", culture)}"))],
            localization.Get("ScheduleDashboard_Empty"));
    }
}

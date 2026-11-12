using System.Globalization;
using Dotnetstore.PersonalApp.Modules.Schedule.Dashboard;
using Dotnetstore.PersonalApp.Shared.Dashboard;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Shell;

namespace Dotnetstore.PersonalApp.Modules.Schedule.Tests;

public sealed class ScheduleDashboardWidgetTests : IDisposable
{
    // 2026-11-02 is a Monday.
    private static readonly DateOnly Monday = new(2026, 11, 2);

    private readonly ScheduleFixture _fixture = new();
    private readonly Guid _teacher = Guid.NewGuid();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public void Dispose() => _fixture.Dispose();

    private ScheduleDashboardWidget Widget()
    {
        var localizer = new Localizer();
        localizer.AddResources(Resources.ScheduleStrings.ResourceManager);
        localizer.SetCulture("en");
        return new ScheduleDashboardWidget(_fixture.DbFactory, _fixture.Subjects, _fixture.Schools, localizer);
    }

    [Fact]
    public async Task ShowsTodaysLessonsInTimeOrderAndTheNumberOfLessonsInTheWeek()
    {
        var maths = _fixture.Subjects.Add("Mathematics");
        var art = _fixture.Subjects.Add("Art");
        var school = _fixture.Schools.Add("Central School");
        await _fixture.CreateAsync(_teacher, DayOfWeek.Monday, "13:00", "14:00", art, school, Ct);
        await _fixture.CreateAsync(_teacher, DayOfWeek.Monday, "08:30", "09:15", maths, school, Ct);
        await _fixture.CreateAsync(_teacher, DayOfWeek.Wednesday, "10:00", "11:00", maths, school, Ct);

        var panel = await Widget().LoadAsync(new DashboardContext(_teacher, Monday), Ct);

        panel.Metrics.Select(m => m.Value).ShouldBe(["2", "3"]);
        panel.Rows.Select(r => r.Title).ShouldBe(["Mathematics", "Art"]);
        panel.Rows[0].Subtitle.ShouldBe("Central School");
        panel.Rows[0].Value.ShouldBe($"{new TimeOnly(8, 30).ToString("t", CultureInfo.GetCultureInfo("en"))}–{new TimeOnly(9, 15).ToString("t", CultureInfo.GetCultureInfo("en"))}");
        panel.IsEmpty.ShouldBeFalse();
    }

    [Fact]
    public async Task OnlyTheSignedInTeachersLessonsAreCounted()
    {
        var maths = _fixture.Subjects.Add("Mathematics");
        var school = _fixture.Schools.Add("Central School");
        await _fixture.CreateAsync(Guid.NewGuid(), DayOfWeek.Monday, "08:00", "09:00", maths, school, Ct);

        var panel = await Widget().LoadAsync(new DashboardContext(_teacher, Monday), Ct);

        panel.Metrics.Select(m => m.Value).ShouldBe(["0", "0"]);
        panel.IsEmpty.ShouldBeTrue();
    }

    [Fact]
    public void TheWidgetBelongsToTheSchoolGroupSoSwitchingItOffHidesIt() =>
        Widget().ModuleKey.ShouldBe(SchoolGroup.ModuleKey);
}

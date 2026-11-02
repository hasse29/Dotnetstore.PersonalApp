using Dotnetstore.PersonalApp.Host.Resources;
using Dotnetstore.PersonalApp.Host.Startup;
using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Startup;

namespace Dotnetstore.PersonalApp.Integration.Tests;

public sealed class StartupRunnerTests
{
    [Fact]
    public async Task RunsTasksInOrderAndReportsProgressUntilReady()
    {
        var order = new List<string>();
        var reports = new List<StartupProgress>();
        var localizer = new Localizer();
        localizer.AddResources(HostStrings.ResourceManager);
        var runner = new StartupRunner([new FakeTask("one", order), new FakeTask("two", order)], localizer);

        await runner.RunAsync(new SyncProgress(reports.Add), TestContext.Current.CancellationToken);

        order.ShouldBe(["one", "two"]);
        reports.Select(r => r.Fraction).ShouldBe([0, 0.5, 1]);
        reports[^1].Message.ShouldBe("Ready");
    }

    private sealed class SyncProgress(Action<StartupProgress> report) : IProgress<StartupProgress>
    {
        public void Report(StartupProgress value) => report(value);
    }

    private sealed class FakeTask(string description, List<string> order) : IStartupTask
    {
        public string Description => description;

        public Task RunAsync(CancellationToken cancellationToken)
        {
            order.Add(description);
            return Task.CompletedTask;
        }
    }
}

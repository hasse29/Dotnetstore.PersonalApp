using Dotnetstore.PersonalApp.Shared.Localization;
using Dotnetstore.PersonalApp.Shared.Startup;
using Dotnetstore.PersonalApp.Shared.Telemetry;

namespace Dotnetstore.PersonalApp.Host.Startup;

internal sealed class StartupRunner(IEnumerable<IStartupTask> tasks, ILocalizationService localization)
{
    private readonly IReadOnlyList<IStartupTask> _tasks = [.. tasks];

    public async Task RunAsync(IProgress<StartupProgress> progress, CancellationToken cancellationToken)
    {
        for (var i = 0; i < _tasks.Count; i++)
        {
            progress.Report(new StartupProgress((double)i / _tasks.Count, _tasks[i].Description));
            var task = _tasks[i];
            await AppTelemetry.TraceAsync(task, () => task.RunAsync(cancellationToken)).ConfigureAwait(false);
        }

        progress.Report(new StartupProgress(1, localization.Get("Startup_Ready")));
    }
}

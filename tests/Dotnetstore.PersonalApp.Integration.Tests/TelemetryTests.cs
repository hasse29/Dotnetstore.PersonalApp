using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using Dotnetstore.PersonalApp.Host;
using Dotnetstore.PersonalApp.Host.Telemetry;
using Dotnetstore.PersonalApp.Shared.Results;
using Dotnetstore.PersonalApp.Shared.Telemetry;

namespace Dotnetstore.PersonalApp.Integration.Tests;

public sealed class TelemetryTests : IDisposable
{
    private readonly List<Activity> _activities = [];
    private readonly ActivityListener _listener;

    public TelemetryTests()
    {
        _listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == AppTelemetry.Name,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStopped = activity =>
            {
                lock (_activities)
                {
                    _activities.Add(activity);
                }
            },
        };
        ActivitySource.AddActivityListener(_listener);
    }

    public void Dispose() => _listener.Dispose();

    private Activity Single(string name)
    {
        lock (_activities)
        {
            return _activities.Single(a => a.OperationName == name);
        }
    }

    [Fact]
    public async Task ASuccessfulHandlerGetsASpanNamedAfterItsFeature()
    {
        var result = await AppTelemetry.TraceAsync(new SuccessFeature.Handler(), () => Task.FromResult(Result<int>.Success(1)));

        result.IsSuccess.ShouldBeTrue();
        Single("SuccessFeature").Status.ShouldBe(ActivityStatusCode.Unset);
    }

    [Fact]
    public async Task AFailedResultMarksTheSpanAsAnErrorWithTheErrorCodeOnly()
    {
        await AppTelemetry.TraceAsync(new FailedFeature.Handler(), () => Task.FromResult(Result<int>.Failure(Error.Validation("x_failed", "secret text"))));

        var span = Single("FailedFeature");
        span.Status.ShouldBe(ActivityStatusCode.Error);
        span.StatusDescription.ShouldBe("x_failed");
    }

    [Fact]
    public async Task AnExceptionIsRecordedOnTheSpanAndRethrown()
    {
        await Should.ThrowAsync<InvalidOperationException>(
            () => AppTelemetry.TraceAsync<int>(new ThrowingFeature.Handler(), () => throw new InvalidOperationException("boom")));

        var span = Single("ThrowingFeature");
        span.Status.ShouldBe(ActivityStatusCode.Error);
        span.Events.ShouldContain(e => e.Name == "exception");
    }

    [Fact]
    public void EveryHandlerRunsThroughTheTelemetryWrapper()
    {
        var handlers = typeof(Program).Assembly.GetReferencedAssemblies()
            .Where(name => name.Name!.StartsWith("Dotnetstore.PersonalApp.Modules.", StringComparison.Ordinal))
            .Select(Assembly.Load)
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => type is { Name: "Handler", IsNested: true })
            .Select(type => type.GetMethod("HandleAsync", BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            .Where(method => method is not null)
            .ToList();

        handlers.ShouldNotBeEmpty();
        // An async HandleAsync would do its work outside AppTelemetry.TraceAsync and so leave no span, metric or log.
        handlers.Where(method => method!.GetCustomAttribute<AsyncStateMachineAttribute>() is not null)
            .Select(method => $"{method!.DeclaringType!.FullName}")
            .ShouldBeEmpty();
    }

    [Fact]
    public void StartingTelemetryWritesALogFileInTheDataFolder()
    {
        var directory = Path.Combine(Path.GetTempPath(), "personalapp-telemetry-" + Guid.NewGuid().ToString("N"));
        try
        {
            using (TelemetrySetup.Start(directory, new UserSettings()))
            {
            }

            var file = Directory.EnumerateFiles(TelemetrySetup.LogDirectory(directory), "personalapp-*.log").Single();
            File.ReadAllText(file).ShouldContain("started");
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }

    [Theory]
    [InlineData("warning", Serilog.Events.LogEventLevel.Warning)]
    [InlineData("nonsense", Serilog.Events.LogEventLevel.Information)]
    [InlineData(null, Serilog.Events.LogEventLevel.Information)]
    public void TheLogLevelSettingFallsBackToInformation(string? value, Serilog.Events.LogEventLevel expected) =>
        TelemetrySetup.ParseLevel(value).ShouldBe(expected);

    [Theory]
    [InlineData("http://localhost:4317", true)]
    [InlineData("not a url", false)]
    [InlineData("ftp://host", false)]
    [InlineData(null, false)]
    public void OnlyAWellFormedHttpEndpointIsExportedTo(string? value, bool valid) =>
        (TelemetrySetup.ParseEndpoint(value) is not null).ShouldBe(valid);

    private static class SuccessFeature { internal sealed class Handler; }

    private static class FailedFeature { internal sealed class Handler; }

    private static class ThrowingFeature { internal sealed class Handler; }
}

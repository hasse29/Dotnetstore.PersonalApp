using System.Reflection;
using Dotnetstore.PersonalApp.Shared.Telemetry;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Extensions.Logging;
using Serilog.Sinks.OpenTelemetry;

namespace Dotnetstore.PersonalApp.Host.Telemetry;

/// <summary>
/// Sets up logging (Serilog, to rolling files in the data folder) and OpenTelemetry (traces and metrics) once, before anything else
/// runs. Everything is also sent to an OTLP endpoint when the user has set one in settings.json; by default nothing leaves the machine.
/// Disposing it flushes the logs and the exporters.
/// </summary>
internal sealed class TelemetrySetup : IDisposable
{
    private const int RetainedLogFiles = 14;

    private readonly TracerProvider _tracing;
    private readonly MeterProvider _metrics;
    private readonly UnhandledExceptionEventHandler _onUnhandled;
    private readonly EventHandler<UnobservedTaskExceptionEventArgs> _onUnobserved;

    private TelemetrySetup(string logDirectory, UserSettings settings)
    {
        var version = typeof(TelemetrySetup).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion?.Split('+')[0] ?? "unknown";
        var level = ParseLevel(settings.LogLevel);
        var endpoint = ParseEndpoint(settings.OtlpEndpoint);

        var levelSwitch = new LoggingLevelSwitch(level);
        var configuration = new LoggerConfiguration()
            .MinimumLevel.ControlledBy(levelSwitch)
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .Enrich.WithProperty("Version", version)
            .WriteTo.File(
                Path.Combine(logDirectory, "personalapp-.log"),
                formatProvider: System.Globalization.CultureInfo.InvariantCulture,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: RetainedLogFiles,
                shared: true,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {SourceContext} {Message:lj} {Properties:j}{NewLine}{Exception}");

        if (endpoint is not null)
        {
            configuration.WriteTo.OpenTelemetry(options =>
            {
                options.Endpoint = endpoint.ToString();
                options.Protocol = OtlpProtocol.Grpc;
                options.ResourceAttributes = new Dictionary<string, object>
                {
                    ["service.name"] = AppTelemetry.Name,
                    ["service.version"] = version,
                };
            });
        }

        Log.Logger = configuration.CreateLogger();
        AppTelemetry.LoggerFactory = new SerilogLoggerFactory(Log.Logger, dispose: false);

        var resource = ResourceBuilder.CreateDefault().AddService(AppTelemetry.Name, serviceVersion: version);

        // The providers always run, so spans carry trace ids into the log; they only export when an endpoint is set.
        var tracing = Sdk.CreateTracerProviderBuilder().SetResourceBuilder(resource).AddSource(AppTelemetry.Name);
        var metrics = Sdk.CreateMeterProviderBuilder().SetResourceBuilder(resource).AddMeter(AppTelemetry.Name).AddRuntimeInstrumentation();
        if (endpoint is not null)
        {
            tracing.AddOtlpExporter(options => options.Endpoint = endpoint);
            metrics.AddOtlpExporter(options => options.Endpoint = endpoint);
        }

        _tracing = tracing.Build();
        _metrics = metrics.Build();

        // The crash that is not caught anywhere else ends up here, so there is always a log of why the app died.
        _onUnhandled = (_, e) =>
        {
            Log.Fatal(e.ExceptionObject as Exception, "Unhandled exception, terminating: {IsTerminating}", e.IsTerminating);
            if (e.IsTerminating)
            {
                Log.CloseAndFlush();
            }
        };
        _onUnobserved = (_, e) => Log.Error(e.Exception, "A task failed and nobody observed it");
        AppDomain.CurrentDomain.UnhandledException += _onUnhandled;
        TaskScheduler.UnobservedTaskException += _onUnobserved;

        Log.Information("Dotnetstore.PersonalApp {Version} started, logging at {Level}, exporting to {Endpoint}", version, level, endpoint?.ToString() ?? "nowhere");
    }

    public static string LogDirectory(string dataDirectory) => Path.Combine(dataDirectory, "logs");

    public static TelemetrySetup Start(string dataDirectory, UserSettings settings) => new(LogDirectory(dataDirectory), settings);

    public static LogEventLevel ParseLevel(string? value) =>
        Enum.TryParse<LogEventLevel>(value, ignoreCase: true, out var level) ? level : LogEventLevel.Information;

    /// <summary>A bad endpoint in settings.json must not stop the app: it just means nothing is exported.</summary>
    public static Uri? ParseEndpoint(string? value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https" ? uri : null;

    public void Dispose()
    {
        AppDomain.CurrentDomain.UnhandledException -= _onUnhandled;
        TaskScheduler.UnobservedTaskException -= _onUnobserved;

        Log.Information("Dotnetstore.PersonalApp stopped");
        _tracing.Dispose();
        _metrics.Dispose();
        AppTelemetry.LoggerFactory = Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance;
        Log.CloseAndFlush();
    }
}

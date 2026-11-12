using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Reflection;
using Dotnetstore.PersonalApp.Shared.Audit;
using Dotnetstore.PersonalApp.Shared.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Dotnetstore.PersonalApp.Shared.Telemetry;

/// <summary>
/// The app's one ActivitySource and Meter, and the wrapper every handler goes through, so each operation gets a span, a duration
/// and a log line on failure without the handler knowing. The Host wires the listeners (OpenTelemetry) and the logger factory
/// (Serilog); without them everything here is a cheap no-op, which is what handler tests get.
/// Nothing about the data being handled is ever recorded: only the operation, the outcome, the error code and the time it took.
/// </summary>
public static class AppTelemetry
{
    public const string Name = "Dotnetstore.PersonalApp";

    public static ActivitySource Source { get; } = new(Name);

    public static Meter Meter { get; } = new(Name);

    private static readonly ConcurrentDictionary<Type, string> OperationNames = new();

    private static readonly Histogram<double> Duration =
        Meter.CreateHistogram<double>("personalapp.operation.duration", "ms", "How long a handler took.");

    private static readonly Counter<long> Failures =
        Meter.CreateCounter<long>("personalapp.operation.failures", description: "Handlers that returned an error or threw.");



    /// <summary>Set once by the Host at startup. Never disposed with a service provider, so it stays valid across tests.</summary>
    public static ILoggerFactory LoggerFactory { get; set; } = NullLoggerFactory.Instance;

    public static ILogger CreateLogger(string category) => LoggerFactory.CreateLogger(category);

    public static ILogger CreateLogger<T>() => LoggerFactory.CreateLogger(typeof(T).FullName ?? typeof(T).Name);

    /// <summary>Runs a handler's work as a traced, timed operation. <paramref name="handler"/> names it (module and feature).</summary>
    public static async Task<T> TraceAsync<T>(object handler, Func<Task<T>> body, object? request = null)
    {
        ArgumentNullException.ThrowIfNull(handler);
        ArgumentNullException.ThrowIfNull(body);

        var operation = OperationName(handler.GetType());
        var logger = CreateLogger(handler.GetType().FullName ?? operation);
        var started = Stopwatch.GetTimestamp();
        using var activity = Source.StartActivity(operation);
        T? outcome = default;
        var succeeded = false;
        string? errorCode = "exception";

        try
        {
            var result = await body().ConfigureAwait(false);
            outcome = result;
            if (result is IResult { IsSuccess: false } failed)
            {
                var code = failed.ErrorCode ?? "unknown";
                errorCode = code;
                activity?.SetStatus(ActivityStatusCode.Error, code);
                activity?.SetTag("error.code", code);
                Failures.Add(1, new KeyValuePair<string, object?>("operation", operation), new KeyValuePair<string, object?>("error.code", code));
                TelemetryLog.OperationFailed(logger, operation, code);
            }
            else
            {
                succeeded = true;
                errorCode = null;
                var elapsed = Stopwatch.GetElapsedTime(started).TotalMilliseconds;
                TelemetryLog.OperationCompleted(logger, operation, elapsed);
            }

            return result;
        }
        catch (OperationCanceledException)
        {
            errorCode = "cancelled";
            activity?.SetStatus(ActivityStatusCode.Error, "cancelled");
            TelemetryLog.OperationCancelled(logger, operation);
            throw;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.GetType().Name);
            activity?.AddException(ex);
            Failures.Add(1, new KeyValuePair<string, object?>("operation", operation), new KeyValuePair<string, object?>("error.code", "exception"));
            TelemetryLog.OperationThrew(logger, ex, operation);
            throw;
        }
        finally
        {
            var totalMs = Stopwatch.GetElapsedTime(started).TotalMilliseconds;
            Duration.Record(totalMs, new KeyValuePair<string, object?>("operation", operation));
            RecordAudit(operation, request, outcome, succeeded, errorCode, totalMs);
        }
    }

    // The audit log does not audit itself (reading it would add a line per look), and an audit failure must never fail the operation.
    private static void RecordAudit(string operation, object? request, object? outcome, bool succeeded, string? errorCode, double elapsedMs)
    {
        if (operation.StartsWith("Audit.", StringComparison.Ordinal))
        {
            return;
        }

        try
        {
            AuditTrail.RecordHandler(operation, request, outcome, succeeded, errorCode, elapsedMs);
        }
        catch (Exception ex) when (ex is InvalidOperationException or TargetInvocationException or ArgumentException)
        {
            TelemetryLog.HandledFailure(CreateLogger(nameof(AuditTrail)), ex, operation);
        }
    }

    /// <summary>The same for work that returns nothing.</summary>
    public static async Task TraceAsync(object handler, Func<Task> body, object? request = null)
    {
        ArgumentNullException.ThrowIfNull(body);

        await TraceAsync<bool>(handler, async () =>
        {
            await body().ConfigureAwait(false);
            return true;
        }, request).ConfigureAwait(false);
    }

    /// <summary>
    /// Logs a failure the code deals with itself (a locked file, a bad network answer), so it still shows up in the log.
    /// Warning level: the app carried on.
    /// </summary>
    public static void LogHandled(this ILogger logger, Exception exception, string operation)
    {
        ArgumentNullException.ThrowIfNull(logger);
        TelemetryLog.HandledFailure(logger, exception, operation);
    }

    /// <summary>"Dotnetstore.PersonalApp.Modules.Sales.Features.Offers.CreateOffer+Handler" becomes "Sales.CreateOffer".</summary>
    internal static string OperationName(Type handlerType) =>
        OperationNames.GetOrAdd(handlerType, static type =>
        {
            var feature = type.DeclaringType?.Name ?? type.Name;
            const string marker = ".Modules.";
            var ns = type.Namespace ?? string.Empty;
            var at = ns.IndexOf(marker, StringComparison.Ordinal);
            if (at < 0)
            {
                return feature;
            }

            var rest = ns[(at + marker.Length)..];
            var dot = rest.IndexOf('.', StringComparison.Ordinal);
            return $"{(dot < 0 ? rest : rest[..dot])}.{feature}";
        });
}

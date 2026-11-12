using Microsoft.Extensions.Logging;

namespace Dotnetstore.PersonalApp.Shared.Telemetry;

/// <summary>Source-generated log messages: structured templates, no string interpolation, no allocation when the level is off.</summary>
internal static partial class TelemetryLog
{
    [LoggerMessage(EventId = 1, Level = LogLevel.Debug, Message = "{Operation} completed in {ElapsedMs:0.0} ms")]
    public static partial void OperationCompleted(ILogger logger, string operation, double elapsedMs);

    [LoggerMessage(EventId = 2, Level = LogLevel.Information, Message = "{Operation} failed with {ErrorCode}")]
    public static partial void OperationFailed(ILogger logger, string operation, string errorCode);

    [LoggerMessage(EventId = 3, Level = LogLevel.Error, Message = "{Operation} threw an exception")]
    public static partial void OperationThrew(ILogger logger, Exception exception, string operation);

    [LoggerMessage(EventId = 4, Level = LogLevel.Debug, Message = "{Operation} was cancelled")]
    public static partial void OperationCancelled(ILogger logger, string operation);

    [LoggerMessage(EventId = 5, Level = LogLevel.Warning, Message = "{Operation} failed and was handled")]
    public static partial void HandledFailure(ILogger logger, Exception exception, string operation);
}

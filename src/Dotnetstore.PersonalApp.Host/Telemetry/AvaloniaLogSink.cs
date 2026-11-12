using Avalonia.Logging;
using Serilog;
using AvaloniaLevel = Avalonia.Logging.LogEventLevel;
using SerilogLevel = Serilog.Events.LogEventLevel;

namespace Dotnetstore.PersonalApp.Host.Telemetry;

/// <summary>Sends Avalonia's own diagnostics (binding errors, layout problems) to Serilog instead of the debug trace.</summary>
internal sealed class AvaloniaLogSink(AvaloniaLevel minimum) : ILogSink
{
    public bool IsEnabled(AvaloniaLevel level, string area) => level >= minimum;

    public void Log(AvaloniaLevel level, string area, object? source, string messageTemplate)
    {
        if (IsEnabled(level, area))
        {
            Serilog.Log.Write(Map(level), "Avalonia {Area}: " + messageTemplate, area);
        }
    }

    public void Log(AvaloniaLevel level, string area, object? source, string messageTemplate, params object?[] propertyValues)
    {
        if (!IsEnabled(level, area))
        {
            return;
        }

        object?[] values = [area, .. propertyValues];
        Serilog.Log.Write(Map(level), "Avalonia {Area}: " + messageTemplate, values);
    }

    private static SerilogLevel Map(AvaloniaLevel level) => level switch
    {
        AvaloniaLevel.Verbose => SerilogLevel.Verbose,
        AvaloniaLevel.Debug => SerilogLevel.Debug,
        AvaloniaLevel.Information => SerilogLevel.Information,
        AvaloniaLevel.Warning => SerilogLevel.Warning,
        AvaloniaLevel.Error => SerilogLevel.Error,
        _ => SerilogLevel.Fatal,
    };
}

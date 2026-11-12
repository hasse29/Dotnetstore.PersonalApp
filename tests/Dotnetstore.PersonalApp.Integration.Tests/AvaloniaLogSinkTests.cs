using Dotnetstore.PersonalApp.Host.Telemetry;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace Dotnetstore.PersonalApp.Integration.Tests;

public sealed class AvaloniaLogSinkTests
{
    [Theory]
    [InlineData(Avalonia.Logging.LogEventLevel.Verbose, false)]
    [InlineData(Avalonia.Logging.LogEventLevel.Information, false)]
    [InlineData(Avalonia.Logging.LogEventLevel.Warning, true)]
    [InlineData(Avalonia.Logging.LogEventLevel.Error, true)]
    public void OnlyWarningsAndWorseAreLetThrough(Avalonia.Logging.LogEventLevel level, bool expected) =>
        new AvaloniaLogSink(Avalonia.Logging.LogEventLevel.Warning).IsEnabled(level, "Binding").ShouldBe(expected);

    [Fact]
    public void ForwardsAvaloniaMessagesToSerilogWithTheAreaAndTheirValues()
    {
        var captured = new CapturingSink();
        var previous = Log.Logger;
        Log.Logger = new LoggerConfiguration().MinimumLevel.Verbose().WriteTo.Sink(captured).CreateLogger();
        try
        {
            var sink = new AvaloniaLogSink(Avalonia.Logging.LogEventLevel.Warning);

            sink.Log(Avalonia.Logging.LogEventLevel.Warning, "Binding", null, "Cannot find {Property}", "Name");
            sink.Log(Avalonia.Logging.LogEventLevel.Information, "Layout", null, "ignored");
        }
        finally
        {
            Log.Logger = previous;
        }

        var written = captured.Events.ShouldHaveSingleItem();
        written.Level.ShouldBe(LogEventLevel.Warning);
        var text = written.RenderMessage(System.Globalization.CultureInfo.InvariantCulture);
        text.ShouldContain("Binding");
        text.ShouldContain("Name");
    }

    private sealed class CapturingSink : ILogEventSink
    {
        public List<LogEvent> Events { get; } = [];

        public void Emit(LogEvent logEvent) => Events.Add(logEvent);
    }
}

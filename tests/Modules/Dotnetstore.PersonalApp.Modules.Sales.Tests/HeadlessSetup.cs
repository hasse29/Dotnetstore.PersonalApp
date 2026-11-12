using Avalonia;
using Avalonia.Headless;
using Avalonia.Themes.Fluent;

namespace Dotnetstore.PersonalApp.Modules.Sales.Tests;

internal sealed class HeadlessApp : Application
{
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<HeadlessApp>().UseHeadless(new AvaloniaHeadlessPlatformOptions());

    public override void Initialize() => Styles.Add(new FluentTheme());
}

/// <summary>One headless Avalonia session for the whole test run; views must be built and used on its UI thread.</summary>
internal static class Headless
{
    private static readonly Lazy<HeadlessUnitTestSession> Session = new(() => HeadlessUnitTestSession.StartNew(typeof(HeadlessApp)));

    public static Task Run(Func<Task> action) => Session.Value.Dispatch(action, CancellationToken.None);
}

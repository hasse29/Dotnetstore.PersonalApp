using Avalonia;
using Avalonia.Headless;

namespace Dotnetstore.PersonalApp.Modules.Email.Tests;

internal sealed class HeadlessApp : Application
{
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<HeadlessApp>().UseHeadless(new AvaloniaHeadlessPlatformOptions());
}

/// <summary>One headless Avalonia session for the whole test run; controls must be built and used on its UI thread.</summary>
internal static class Headless
{
    private static readonly Lazy<HeadlessUnitTestSession> Session = new(() => HeadlessUnitTestSession.StartNew(typeof(HeadlessApp)));

    public static Task Run(Action action) => Session.Value.Dispatch(() => { action(); return 0; }, CancellationToken.None);

    public static Task<T> Run<T>(Func<T> func) => Session.Value.Dispatch(func, CancellationToken.None);
}

using System.Net;
using System.Text;
using Dotnetstore.PersonalApp.Host.Updates;
using Dotnetstore.PersonalApp.Host.ViewModels;
using Dotnetstore.PersonalApp.Shared.Localization;

namespace Dotnetstore.PersonalApp.Integration.Tests;

// The fake handlers live as long as the test; there is nothing to release.
#pragma warning disable CA2000
public sealed class UpdateCheckTests
{
    private static readonly Version Running = new(6, 1, 0);

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Theory]
    [InlineData("v6.2.0", "6.2.0")]
    [InlineData("6.1.1", "6.1.1")]
    [InlineData("v7.0.0", "7.0.0")]
    [InlineData("6.2.0+abc123", "6.2.0")]
    public void ANewerTagIsOfferedWithItsVersion(string tag, string expected)
    {
        var update = GitHubUpdateChecker.Evaluate(tag, null, Running);

        update.ShouldNotBeNull().Version.ShouldBe(Version.Parse(expected));
    }

    [Theory]
    [InlineData("v6.1.0")]
    [InlineData("v6.0.9")]
    [InlineData("v5.0.0")]
    [InlineData("latest")]
    [InlineData("")]
    [InlineData(null)]
    public void AnOlderEqualOrUnreadableTagIsNotOffered(string? tag) =>
        GitHubUpdateChecker.Evaluate(tag, null, Running).ShouldBeNull();

    [Fact]
    public void OnlyLinksToThisRepositoryAreKept()
    {
        var own = GitHubUpdateChecker.ReleasesPage + "/tag/v6.2.0";

        GitHubUpdateChecker.Evaluate("v6.2.0", own, Running)!.Url.ShouldBe(own);
        GitHubUpdateChecker.Evaluate("v6.2.0", "https://evil.example/download.exe", Running)!.Url.ShouldBe(GitHubUpdateChecker.ReleasesPage);
        GitHubUpdateChecker.Evaluate("v6.2.0", null, Running)!.Url.ShouldBe(GitHubUpdateChecker.ReleasesPage);
    }

    [Fact]
    public void TheRunningVersionIsReadFromTheBuild() =>
        GitHubUpdateChecker.Current.ShouldBeGreaterThan(new Version(0, 0, 0));

    [Fact]
    public async Task AReleaseNewerThanTheRunningVersionIsFound()
    {
        var next = new Version(GitHubUpdateChecker.Current.Major + 1, 0, 0);
        var checker = CheckerReplying(HttpStatusCode.OK, $$"""{"tag_name":"v{{next}}","html_url":"{{GitHubUpdateChecker.ReleasesPage}}/tag/v{{next}}"}""");

        var update = await checker.CheckAsync(Ct);

        update.ShouldNotBeNull().Version.ShouldBe(next);
    }

    [Theory]
    [InlineData(HttpStatusCode.NotFound, "{}")]
    [InlineData(HttpStatusCode.Forbidden, "{\"message\":\"rate limit\"}")]
    [InlineData(HttpStatusCode.OK, "not json")]
    public async Task AFailedCheckIsSilent(HttpStatusCode status, string body) =>
        (await CheckerReplying(status, body).CheckAsync(Ct)).ShouldBeNull();

    [Fact]
    public async Task AnUnreachableGitHubIsSilent()
    {
        var checker = new GitHubUpdateChecker(new FakeClientFactory(new ThrowingHandler()));

        (await checker.CheckAsync(Ct)).ShouldBeNull();
    }

    [Fact]
    public async Task TheBannerAppearsForANewerVersionAndStaysHiddenOnceDismissed()
    {
        var launcher = new RecordingLauncher();
        var banner = new UpdateBannerViewModel(new FixedChecker(new UpdateInfo(new Version(9, 0, 0), GitHubUpdateChecker.ReleasesPage + "/tag/v9.0.0")), launcher, new UpdateOptions(true), HostLocalizer());

        banner.IsVisible.ShouldBeFalse();
        await banner.CheckCommand.ExecuteAsync(null);
        banner.IsVisible.ShouldBeTrue();
        banner.Message.ShouldContain("9.0.0");

        await banner.DownloadCommand.ExecuteAsync(null);
        launcher.Opened.ShouldHaveSingleItem().AbsoluteUri.ShouldEndWith("/tag/v9.0.0");

        banner.DismissCommand.Execute(null);
        banner.IsVisible.ShouldBeFalse();
        await banner.CheckCommand.ExecuteAsync(null);
        banner.IsVisible.ShouldBeFalse();
    }

    [Fact]
    public async Task NothingIsAskedWhenTheCheckIsSwitchedOff()
    {
        var checker = new FixedChecker(new UpdateInfo(new Version(9, 0, 0), GitHubUpdateChecker.ReleasesPage));
        var banner = new UpdateBannerViewModel(checker, new RecordingLauncher(), new UpdateOptions(false), HostLocalizer());

        await banner.CheckCommand.ExecuteAsync(null);

        banner.IsVisible.ShouldBeFalse();
        checker.Calls.ShouldBe(0);
    }

    [Fact]
    public async Task TheCheckIsMadeOncePerRun()
    {
        var checker = new FixedChecker(null);
        var banner = new UpdateBannerViewModel(checker, new RecordingLauncher(), new UpdateOptions(true), HostLocalizer());

        await banner.CheckCommand.ExecuteAsync(null);
        await banner.CheckCommand.ExecuteAsync(null);

        checker.Calls.ShouldBe(1);
        banner.IsVisible.ShouldBeFalse();
    }

    private static Localizer HostLocalizer()
    {
        var localizer = new Localizer();
        localizer.AddResources(Dotnetstore.PersonalApp.Host.Resources.HostStrings.ResourceManager);
        return localizer;
    }

    private static GitHubUpdateChecker CheckerReplying(HttpStatusCode status, string body) =>
        new(new FakeClientFactory(new ReplyingHandler(status, body)));

    private sealed class FixedChecker(UpdateInfo? update) : IUpdateChecker
    {
        public int Calls { get; private set; }

        public Task<UpdateInfo?> CheckAsync(CancellationToken cancellationToken)
        {
            Calls++;
            return Task.FromResult(update);
        }
    }

    private sealed class RecordingLauncher : IUriLauncher
    {
        public List<Uri> Opened { get; } = [];

        public Task LaunchAsync(Uri uri)
        {
            Opened.Add(uri);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeClientFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
    }

    private sealed class ReplyingHandler(HttpStatusCode status, string body) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") });
    }

    private sealed class ThrowingHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            throw new HttpRequestException("no network");
    }
}

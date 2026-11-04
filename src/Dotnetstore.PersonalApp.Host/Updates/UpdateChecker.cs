using System.Net.Http.Headers;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Dotnetstore.PersonalApp.Host.Updates;

/// <summary>A release newer than the running app. <paramref name="Url"/> is the page to download it from.</summary>
internal sealed record UpdateInfo(Version Version, string Url);

internal interface IUpdateChecker
{
    /// <returns>The newer release, or null when the app is up to date or the check could not be made.</returns>
    Task<UpdateInfo?> CheckAsync(CancellationToken cancellationToken);
}

/// <summary>Asks GitHub for the latest release and compares it with the running version. A check that fails is silent: it is never worth interrupting startup for.</summary>
internal sealed class GitHubUpdateChecker(IHttpClientFactory httpClientFactory) : IUpdateChecker
{
    public const string ClientName = "updates";

    public const string ReleasesPage = "https://github.com/hasse29/Dotnetstore.PersonalApp/releases";

    private const string LatestRelease = "https://api.github.com/repos/hasse29/Dotnetstore.PersonalApp/releases/latest";

    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(8);

    /// <summary>The version this build was made from (<c>&lt;Version&gt;</c> in Directory.Build.props).</summary>
    public static Version Current { get; } = ParseVersion(typeof(GitHubUpdateChecker).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion)
        ?? new Version(0, 0, 0);

    public async Task<UpdateInfo?> CheckAsync(CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(Timeout);

        try
        {
            using var client = httpClientFactory.CreateClient(ClientName);
            using var request = new HttpRequestMessage(HttpMethod.Get, LatestRelease);
            request.Headers.UserAgent.Add(new ProductInfoHeaderValue("Dotnetstore.PersonalApp", Current.ToString(3)));
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));

            using var response = await client.SendAsync(request, timeout.Token).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            await using var body = await response.Content.ReadAsStreamAsync(timeout.Token).ConfigureAwait(false);
            var release = await JsonSerializer.DeserializeAsync<Release>(body, cancellationToken: timeout.Token).ConfigureAwait(false);

            return Evaluate(release?.Tag, release?.Url, Current);
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or OperationCanceledException && !cancellationToken.IsCancellationRequested)
        {
            return null;
        }
    }

    /// <summary>The update to offer for a release tag such as <c>v6.2.0</c>, or null when it is not newer than <paramref name="current"/>.</summary>
    public static UpdateInfo? Evaluate(string? tag, string? url, Version current)
    {
        if (ParseVersion(tag) is not { } latest || latest <= current)
        {
            return null;
        }

        // The link comes off the network and is opened in the browser, so only this repository's own pages are trusted.
        var page = url is not null && url.StartsWith(ReleasesPage + "/", StringComparison.Ordinal) ? url : ReleasesPage;
        return new UpdateInfo(latest, page);
    }

    /// <summary>Reads <c>6.1.0</c>, <c>v6.1.0</c> or <c>6.1.0+abc123</c>; anything else is not a version.</summary>
    public static Version? ParseVersion(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var core = text.Trim().TrimStart('v', 'V');
        var end = core.IndexOfAny(['+', '-']);
        if (end >= 0)
        {
            core = core[..end];
        }

        return Version.TryParse(core, out var version) ? version : null;
    }

    private sealed record Release([property: JsonPropertyName("tag_name")] string? Tag, [property: JsonPropertyName("html_url")] string? Url);
}

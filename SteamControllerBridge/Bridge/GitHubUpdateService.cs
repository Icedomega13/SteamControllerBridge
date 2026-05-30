using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace SteamControllerBridge.Bridge;

internal static class GitHubUpdateService
{
    private const string LatestReleaseUrl = "https://api.github.com/repos/Icedomega13/SteamControllerBridge/releases/latest";
    private static readonly HttpClient Http = new()
    {
        Timeout = TimeSpan.FromSeconds(20)
    };

    static GitHubUpdateService()
    {
        Http.DefaultRequestHeaders.UserAgent.ParseAdd("SteamControllerBridge-Updater");
        Http.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
    }

    public static Version CurrentVersion => typeof(GitHubUpdateService).Assembly.GetName().Version ?? new Version(0, 0, 0);

    public static async Task<UpdateCheckResult> CheckForUpdatesAsync(CancellationToken token)
    {
        var release = await Http.GetFromJsonAsync<GitHubRelease>(LatestReleaseUrl, cancellationToken: token)
            ?? throw new InvalidOperationException("GitHub did not return release information.");

        if (!TryParseVersion(release.TagName, out var latestVersion))
        {
            throw new InvalidOperationException($"Could not read release version '{release.TagName}'.");
        }

        var current = NormalizeVersion(CurrentVersion);
        var latest = NormalizeVersion(latestVersion);
        var installer = release.Assets
            .Where(asset => asset.Name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            .Where(asset => asset.Name.Contains("Setup", StringComparison.OrdinalIgnoreCase)
                || asset.Name.Contains("Installer", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(asset => asset.Name.Contains(latest.ToString(), StringComparison.OrdinalIgnoreCase))
            .FirstOrDefault();

        var zip = release.Assets
            .FirstOrDefault(asset => asset.Name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase));

        return new UpdateCheckResult(
            CurrentVersion: current,
            LatestVersion: latest,
            IsUpdateAvailable: latest.CompareTo(current) > 0,
            ReleaseUrl: release.HtmlUrl,
            AssetName: installer?.Name ?? zip?.Name ?? string.Empty,
            AssetDownloadUrl: installer?.BrowserDownloadUrl ?? zip?.BrowserDownloadUrl ?? string.Empty,
            IsInstaller: installer is not null);
    }

    public static async Task<string> DownloadUpdateAsync(UpdateCheckResult update, IProgress<int>? progress, CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(update.AssetDownloadUrl))
        {
            throw new InvalidOperationException("The latest release does not include a downloadable update asset.");
        }

        var extension = Path.GetExtension(update.AssetName);
        var fileName = string.IsNullOrWhiteSpace(update.AssetName)
            ? $"SteamControllerBridgeUpdate-{update.LatestVersion}{extension}"
            : update.AssetName;
        var directory = Path.Combine(Path.GetTempPath(), "SteamControllerBridge", "Updates", update.LatestVersion.ToString());
        Directory.CreateDirectory(directory);
        var destination = Path.Combine(directory, fileName);

        using var response = await Http.GetAsync(update.AssetDownloadUrl, HttpCompletionOption.ResponseHeadersRead, token);
        response.EnsureSuccessStatusCode();

        var total = response.Content.Headers.ContentLength;
        await using var source = await response.Content.ReadAsStreamAsync(token);
        await using var target = File.Create(destination);
        var buffer = new byte[128 * 1024];
        long downloaded = 0;
        int read;
        while ((read = await source.ReadAsync(buffer, token)) > 0)
        {
            await target.WriteAsync(buffer.AsMemory(0, read), token);
            downloaded += read;
            if (total is > 0)
            {
                progress?.Report((int)Math.Clamp(downloaded * 100 / total.Value, 0, 100));
            }
        }

        progress?.Report(100);
        return destination;
    }

    public static void LaunchInstaller(string path)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = path,
            UseShellExecute = true
        });
    }

    public static void OpenReleasePage(string releaseUrl)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = releaseUrl,
            UseShellExecute = true
        });
    }

    private static bool TryParseVersion(string tag, out Version version)
    {
        tag = tag.Trim().TrimStart('v', 'V');
        return Version.TryParse(tag, out version!);
    }

    private static Version NormalizeVersion(Version version)
    {
        return new Version(
            Math.Max(version.Major, 0),
            Math.Max(version.Minor, 0),
            Math.Max(version.Build, 0));
    }

    private sealed class GitHubRelease
    {
        [JsonPropertyName("tag_name")]
        public string TagName { get; set; } = string.Empty;

        [JsonPropertyName("html_url")]
        public string HtmlUrl { get; set; } = string.Empty;

        [JsonPropertyName("assets")]
        public List<GitHubAsset> Assets { get; set; } = new();
    }

    private sealed class GitHubAsset
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("browser_download_url")]
        public string BrowserDownloadUrl { get; set; } = string.Empty;
    }
}

internal sealed record UpdateCheckResult(
    Version CurrentVersion,
    Version LatestVersion,
    bool IsUpdateAvailable,
    string ReleaseUrl,
    string AssetName,
    string AssetDownloadUrl,
    bool IsInstaller);

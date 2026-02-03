using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;

namespace MistXI.Launcher.Services;

public sealed class GitHubXiLoaderService
{
    private readonly HttpClient _http;

    public GitHubXiLoaderService()
    {
        _http = new HttpClient();
        _http.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("MistXI-Launcher", "0.1"));
    }

    private sealed class ReleaseAsset
    {
        public string? name { get; set; }
        public string? browser_download_url { get; set; }
    }
    private sealed class Release
    {
        public string? tag_name { get; set; }
        public List<ReleaseAsset>? assets { get; set; }
    }

    public async Task<string> EnsureLatestXiLoaderAsync(string destExePath, IProgress<string>? progress = null, CancellationToken ct = default)
    {
        progress?.Report("Checking latest XiLoader release…");
        var rel = await GetLatestReleaseAsync(ct);

        if (rel.assets is null || rel.assets.Count == 0)
            throw new InvalidOperationException("Latest XiLoader release has no assets.");

        var asset =
            rel.assets.FirstOrDefault(a => string.Equals(a.name, "xiloader.exe", StringComparison.OrdinalIgnoreCase))
            ?? rel.assets.FirstOrDefault(a => (a.name ?? "").ToLowerInvariant().Contains("xiloader") && (a.name ?? "").ToLowerInvariant().EndsWith(".exe"))
            ?? rel.assets.FirstOrDefault(a => (a.name ?? "").ToLowerInvariant().EndsWith(".exe"));

        if (asset?.browser_download_url is null)
            throw new InvalidOperationException("Could not find a downloadable xiloader.exe asset in the latest release.");

        Directory.CreateDirectory(Path.GetDirectoryName(destExePath)!);

        var tmp = destExePath + ".tmp";
        progress?.Report("Downloading XiLoader…");
        await DownloadAsync(asset.browser_download_url, tmp, ct);

        var fi = new FileInfo(tmp);
        if (fi.Length < 50_000)
            throw new InvalidOperationException("Downloaded XiLoader looks too small; aborting.");

        if (File.Exists(destExePath)) File.Delete(destExePath);
        File.Move(tmp, destExePath);

        progress?.Report($"XiLoader ready ({rel.tag_name}).");
        return rel.tag_name ?? "latest";
    }

    private async Task<Release> GetLatestReleaseAsync(CancellationToken ct)
    {
        const string url = "https://api.github.com/repos/LandSandBoat/xiloader/releases/latest";
        using var resp = await _http.GetAsync(url, ct);
        resp.EnsureSuccessStatusCode();
        var json = await resp.Content.ReadAsStringAsync(ct);
        return JsonSerializer.Deserialize<Release>(json) ?? throw new InvalidOperationException("Failed to parse GitHub release JSON.");
    }

    private static async Task DownloadAsync(string url, string outPath, CancellationToken ct)
    {
        using var http = new HttpClient();
        http.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("MistXI-Launcher", "0.1"));
        using var resp = await http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
        resp.EnsureSuccessStatusCode();
        await using var fs = File.Create(outPath);
        await resp.Content.CopyToAsync(fs, ct);
    }
}

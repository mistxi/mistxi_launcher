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

    public async Task<string> EnsureLatestXiLoaderAsync(string destExePath, IProgress<string>? progress = null, CancellationToken ct = default, string? versionOverride = null)
    {
        progress?.Report("Checking XiLoader release…");
        
        // Treat null, empty, or non-tag strings (e.g. "Latest (default)") as "use latest"
        var useOverride = !string.IsNullOrWhiteSpace(versionOverride) 
                          && versionOverride.StartsWith("v", StringComparison.OrdinalIgnoreCase);
        
        Release rel;
        if (useOverride)
        {
            progress?.Report($"Using pinned XiLoader version: {versionOverride}");
            rel = await GetReleaseByTagAsync(versionOverride!, ct);
        }
        else
        {
            rel = await GetLatestReleaseAsync(ct);
        }

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
    
    private async Task<Release> GetReleaseByTagAsync(string tag, CancellationToken ct)
    {
        var url = $"https://api.github.com/repos/LandSandBoat/xiloader/releases/tags/{tag}";
        using var resp = await _http.GetAsync(url, ct);
        resp.EnsureSuccessStatusCode();
        var json = await resp.Content.ReadAsStringAsync(ct);
        return JsonSerializer.Deserialize<Release>(json) ?? throw new InvalidOperationException("Failed to parse GitHub release JSON.");
    }
    
    public async Task<List<string>> GetRecentReleaseTagsAsync(int count = 5, CancellationToken ct = default)
    {
        try
        {
            const string url = "https://api.github.com/repos/LandSandBoat/xiloader/releases";
            using var resp = await _http.GetAsync(url, ct);
            resp.EnsureSuccessStatusCode();
            var json = await resp.Content.ReadAsStringAsync(ct);
            var releases = JsonSerializer.Deserialize<List<Release>>(json) ?? new();
            return releases
                .Where(r => !string.IsNullOrEmpty(r.tag_name))
                .Take(count)
                .Select(r => r.tag_name!)
                .ToList();
        }
        catch
        {
            return new List<string>();
        }
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

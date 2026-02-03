namespace MistXI.Launcher.Services;

/// <summary>
/// Downloads and maintains a portable Ashita v4 runtime under runtime/ashita.
/// Ashita v4 beta is distributed as a repository snapshot zip (no GitHub "Releases").
/// We track updates via the latest commit on the main branch.
/// </summary>
public sealed class GitHubAshitaService
{
    private readonly HttpClient _http;

    public GitHubAshitaService()
    {
        _http = new HttpClient();
        _http.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("MistXI-Launcher", "0.1"));
        _http.Timeout = TimeSpan.FromMinutes(10);
    }

    private sealed class CommitInfo
    {
        public string? sha { get; set; }
    }

    public async Task EnsureLatestAshitaAsync(string ashitaDir, IProgress<string>? progress = null, CancellationToken ct = default)
    {
        progress?.Report("Checking Ashita updates…");

        // Ashita v4 beta is installed from the repository snapshot zip:
        // https://github.com/AshitaXI/Ashita-v4beta/archive/refs/heads/main.zip
        // We use the latest main-branch commit SHA as our version marker.
        var latestSha = await GetMainCommitShaAsync(ct);
        var version = string.IsNullOrWhiteSpace(latestSha) ? null : latestSha.Substring(0, Math.Min(12, latestSha.Length));
        if (string.IsNullOrWhiteSpace(version))
            throw new InvalidOperationException("Could not determine the latest Ashita version (main commit SHA).");

        var versionFile = Path.Combine(ashitaDir, "version.txt");
        var localVersion = File.Exists(versionFile) ? File.ReadAllText(versionFile).Trim() : null;

        var ashitaExe = Path.Combine(ashitaDir, "Ashita-cli.exe");
        if (File.Exists(ashitaExe) && string.Equals(localVersion, version, StringComparison.OrdinalIgnoreCase))
        {
            progress?.Report($"Ashita is up to date ({version}).");
            return;
        }

        progress?.Report($"Downloading Ashita (main @ {version})…");

        var tmpRoot = Path.Combine(Path.GetTempPath(), "MistXI_Ashita_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tmpRoot);
        var zipPath = Path.Combine(tmpRoot, "ashita.zip");
        var extractPath = Path.Combine(tmpRoot, "extract");
        Directory.CreateDirectory(extractPath);

        try
        {
            await DownloadFileAsync("https://github.com/AshitaXI/Ashita-v4beta/archive/refs/heads/main.zip", zipPath, progress, ct);

            progress?.Report("Extracting Ashita…");
            ZipFile.ExtractToDirectory(zipPath, extractPath, overwriteFiles: true);

            // Normalize extracted root. The snapshot zip contains a single top-level folder.
            var normalizedRoot = FindAshitaRoot(extractPath);

            // Preserve user directories on update.
            var preserve = new[]
            {
                ("addons", Path.Combine(ashitaDir, "addons")),
                ("plugins", Path.Combine(ashitaDir, "plugins")),
                (Path.Combine("config","boot"), Path.Combine(ashitaDir, "config", "boot")),
                (Path.Combine("config","profiles"), Path.Combine(ashitaDir, "config", "profiles")),
            };

            var preserveTmp = Path.Combine(tmpRoot, "preserve");
            Directory.CreateDirectory(preserveTmp);

            if (Directory.Exists(ashitaDir))
            {
                progress?.Report("Preparing Ashita update…");
                foreach (var (key, src) in preserve)
                {
                    if (Directory.Exists(src))
                    {
                        var dst = Path.Combine(preserveTmp, key);
                        CopyDirectory(src, dst, overwrite: true);
                    }
                }

                try { Directory.Delete(ashitaDir, recursive: true); } catch { /* best effort */ }
            }

            Directory.CreateDirectory(ashitaDir);

            // Copy extracted runtime into ashitaDir
            CopyDirectory(normalizedRoot, ashitaDir, overwrite: true);

            // Restore preserved dirs
            foreach (var (key, _) in preserve)
            {
                var src = Path.Combine(preserveTmp, key);
                if (Directory.Exists(src))
                {
                    var dst = Path.Combine(ashitaDir, key);
                    CopyDirectory(src, dst, overwrite: true);
                }
            }

            // Ensure required subfolders exist (for our launcher)
            Directory.CreateDirectory(Path.Combine(ashitaDir, "bootloader"));
            Directory.CreateDirectory(Path.Combine(ashitaDir, "config", "boot"));

            // Verify
            if (!File.Exists(Path.Combine(ashitaDir, "Ashita-cli.exe")))
                throw new InvalidOperationException("Ashita extraction completed but Ashita-cli.exe was not found.");

            // Install custom addons and plugins
            progress?.Report("Installing custom addons and plugins...");
            await InstallCustomAddonsAsync(ashitaDir, progress, ct);
            
            // Remove unwanted addons and plugins
            RemoveUnwantedAddons(ashitaDir);

            File.WriteAllText(versionFile, version);

            progress?.Report($"Ashita installed ({version}).");
        }
        finally
        {
            try { Directory.Delete(tmpRoot, recursive: true); } catch { /* ignore */ }
        }
    }
    
    private async Task InstallCustomAddonsAsync(string ashitaDir, IProgress<string>? progress, CancellationToken ct)
    {
        try
        {
            // Install NoMount addon
            progress?.Report("Downloading NoMount addon...");
            await InstallNoMountAddonAsync(ashitaDir, ct);
            
            // Install Deeps plugin
            progress?.Report("Downloading Deeps plugin...");
            await InstallDeepsPluginAsync(ashitaDir, ct);
        }
        catch (Exception ex)
        {
            // Non-critical, log but don't fail
            progress?.Report($"Warning: Failed to install custom addons: {ex.Message}");
        }
    }
    
    private async Task InstallNoMountAddonAsync(string ashitaDir, CancellationToken ct)
    {
        // Download from: https://github.com/ThornyFFXI/MiscAshita4/tree/main/addons/NoMount
        var addonDir = Path.Combine(ashitaDir, "addons", "nomount");
        Directory.CreateDirectory(addonDir);
        
        // Download nomount.lua
        var luaUrl = "https://raw.githubusercontent.com/ThornyFFXI/MiscAshita4/main/addons/NoMount/nomount.lua";
        var luaContent = await _http.GetStringAsync(luaUrl, ct);
        File.WriteAllText(Path.Combine(addonDir, "nomount.lua"), luaContent);
    }
    
    private async Task InstallDeepsPluginAsync(string ashitaDir, CancellationToken ct)
    {
        // Download from: https://github.com/relliko/Deeps/releases
        var pluginsDir = Path.Combine(ashitaDir, "plugins");
        Directory.CreateDirectory(pluginsDir);
        
        // Get latest release
        var releasesUrl = "https://api.github.com/repos/relliko/Deeps/releases/latest";
        var releaseJson = await _http.GetStringAsync(releasesUrl, ct);
        
        // Parse to find deeps.dll download URL
        var match = System.Text.RegularExpressions.Regex.Match(releaseJson, @"""browser_download_url""\s*:\s*""([^""]*deeps\.dll)""");
        if (!match.Success)
            throw new InvalidOperationException("Could not find deeps.dll in latest release");
        
        var dllUrl = match.Groups[1].Value;
        var dllBytes = await _http.GetByteArrayAsync(dllUrl, ct);
        File.WriteAllBytes(Path.Combine(pluginsDir, "deeps.dll"), dllBytes);
    }
    
    private void RemoveUnwantedAddons(string ashitaDir)
    {
        // Remove unwanted addons
        var unwantedAddons = new[] { "skeletonkey", "ahgo", "allmaps", "instantah", "instantchat", "paranormal" };
        var addonsDir = Path.Combine(ashitaDir, "addons");
        
        foreach (var unwanted in unwantedAddons)
        {
            var path = Path.Combine(addonsDir, unwanted);
            if (Directory.Exists(path))
            {
                try
                {
                    Directory.Delete(path, recursive: true);
                }
                catch { /* ignore if can't delete */ }
            }
        }
    }

    private async Task<string> GetMainCommitShaAsync(CancellationToken ct)
    {
        // GitHub API: latest commit on main.
        var url = "https://api.github.com/repos/AshitaXI/Ashita-v4beta/commits/main";
        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        using var resp = await _http.SendAsync(req, ct);
        resp.EnsureSuccessStatusCode();
        var json = await resp.Content.ReadAsStringAsync(ct);
        var info = JsonSerializer.Deserialize<CommitInfo>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        return info?.sha ?? "";
    }

    private async Task DownloadFileAsync(string url, string destPath, IProgress<string>? progress, CancellationToken ct)
    {
        using var resp = await _http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
        resp.EnsureSuccessStatusCode();

        Directory.CreateDirectory(Path.GetDirectoryName(destPath)!);

        await using var fs = File.Create(destPath);
        await using var stream = await resp.Content.ReadAsStreamAsync(ct);

        var buffer = new byte[1024 * 64];
        long total = 0;
        var len = resp.Content.Headers.ContentLength;

        while (true)
        {
            var read = await stream.ReadAsync(buffer, 0, buffer.Length, ct);
            if (read <= 0) break;
            await fs.WriteAsync(buffer, 0, read, ct);
            total += read;

            if (len.HasValue && len.Value > 0)
            {
                var pct = (int)(total * 100 / len.Value);
                progress?.Report($"Downloading Ashita… {pct}%");
            }
        }
    }

    private static string FindAshitaRoot(string extractPath)
    {
        // If extractPath contains Ashita-cli.exe anywhere, prefer the folder that contains it.
        var exe = Directory.GetFiles(extractPath, "Ashita-cli.exe", SearchOption.AllDirectories).FirstOrDefault();
        if (exe is not null)
        {
            return Path.GetDirectoryName(exe)!;
        }

        // Otherwise if there is a single directory, use it.
        var dirs = Directory.GetDirectories(extractPath);
        if (dirs.Length == 1) return dirs[0];

        // Otherwise, use extractPath.
        return extractPath;
    }

    private static void CopyDirectory(string sourceDir, string destDir, bool overwrite)
    {
        Directory.CreateDirectory(destDir);

        foreach (var file in Directory.GetFiles(sourceDir))
        {
            var name = Path.GetFileName(file);
            var dest = Path.Combine(destDir, name);
            File.Copy(file, dest, overwrite);
        }

        foreach (var dir in Directory.GetDirectories(sourceDir))
        {
            var name = Path.GetFileName(dir);
            var dest = Path.Combine(destDir, name);
            CopyDirectory(dir, dest, overwrite);
        }
    }
}

using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;

namespace MistXI.Launcher.Services;

public sealed class LauncherUpdater
{
    private readonly Logger _log;
    private readonly HttpClient _http;
    private const string GitHubApiUrl = "https://api.github.com/repos/mistxi/mistxi_launcher/releases/latest";
    
    private static string CurrentVersion
    {
        get
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            return version != null ? $"{version.Major}.{version.Minor}.{version.Build}" : "1.1.0";
        }
    }
    
    public LauncherUpdater(Logger logger)
    {
        _log = logger;
        _http = new HttpClient();
        _http.DefaultRequestHeaders.UserAgent.ParseAdd("MistXI-Launcher/1.1.0");
    }
    
    public async Task<UpdateInfo?> CheckForUpdateAsync()
    {
        try
        {
            _log.Write("Checking for launcher updates...");
            
            var response = await _http.GetStringAsync(GitHubApiUrl);
            var release = JsonSerializer.Deserialize<GitHubRelease>(response);
            
            if (release == null || string.IsNullOrEmpty(release.tag_name))
            {
                _log.Write("Could not parse GitHub release info");
                return null;
            }
            
            // Strip 'v' prefix if present (e.g., "v1.1.0" -> "1.1.0")
            var latestVersion = release.tag_name.TrimStart('v');
            
            _log.Write($"Current version: {CurrentVersion}, Latest version: {latestVersion}");
            
            if (IsNewerVersion(CurrentVersion, latestVersion))
            {
                // Find the .exe asset
                var exeAsset = release.assets?.FirstOrDefault(a => 
                    a.name?.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) == true);
                
                if (exeAsset != null && !string.IsNullOrEmpty(exeAsset.browser_download_url))
                {
                    return new UpdateInfo
                    {
                        Version = latestVersion,
                        DownloadUrl = exeAsset.browser_download_url,
                        ReleaseNotes = release.body ?? "No release notes available.",
                        PublishedAt = release.published_at
                    };
                }
            }
            
            _log.Write("Launcher is up to date");
            return null;
        }
        catch (Exception ex)
        {
            _log.Write("Failed to check for updates", ex);
            return null;
        }
    }
    
    public async Task<bool> DownloadAndApplyUpdateAsync(string downloadUrl, IProgress<string>? progress = null)
    {
        try
        {
            progress?.Report("Downloading update...");
            _log.Write($"Downloading update from: {downloadUrl}");
            
            var tempFile = Path.Combine(Path.GetTempPath(), "MistXILauncher_update.exe");
            
            // Download new version
            var data = await _http.GetByteArrayAsync(downloadUrl);
            await File.WriteAllBytesAsync(tempFile, data);
            
            _log.Write($"Downloaded update to: {tempFile}");
            progress?.Report("Applying update...");
            
            // Get current exe path
            var currentExe = Process.GetCurrentProcess().MainModule?.FileName;
            if (string.IsNullOrEmpty(currentExe))
            {
                _log.Write("Could not determine current exe path");
                return false;
            }
            
            // Create a batch script to replace the exe after launcher exits
            var batchFile = Path.Combine(Path.GetTempPath(), "MistXI_Update.bat");
            var batchContent = $@"@echo off
echo Updating MistXI Launcher...
timeout /t 2 /nobreak > nul
move /y ""{tempFile}"" ""{currentExe}""
start """" ""{currentExe}""
del ""%~f0""
";
            
            await File.WriteAllTextAsync(batchFile, batchContent);
            
            _log.Write("Starting update process...");
            progress?.Report("Restarting launcher...");
            
            // Start the batch file and exit
            var psi = new ProcessStartInfo
            {
                FileName = batchFile,
                CreateNoWindow = true,
                UseShellExecute = false
            };
            
            Process.Start(psi);
            
            // Exit the current launcher so the batch can replace it
            await Task.Delay(500); // Give batch time to start
            Environment.Exit(0);
            
            return true;
        }
        catch (Exception ex)
        {
            _log.Write("Failed to apply update", ex);
            return false;
        }
    }
    
    private bool IsNewerVersion(string current, string latest)
    {
        try
        {
            var currentParts = current.Split('.').Select(int.Parse).ToArray();
            var latestParts = latest.Split('.').Select(int.Parse).ToArray();
            
            for (int i = 0; i < Math.Min(currentParts.Length, latestParts.Length); i++)
            {
                if (latestParts[i] > currentParts[i]) return true;
                if (latestParts[i] < currentParts[i]) return false;
            }
            
            return latestParts.Length > currentParts.Length;
        }
        catch
        {
            return false;
        }
    }
    
    private class GitHubRelease
    {
        public string? tag_name { get; set; }
        public string? body { get; set; }
        public DateTime published_at { get; set; }
        public List<GitHubAsset>? assets { get; set; }
    }
    
    private class GitHubAsset
    {
        public string? name { get; set; }
        public string? browser_download_url { get; set; }
    }
}

public class UpdateInfo
{
    public string Version { get; set; } = "";
    public string DownloadUrl { get; set; } = "";
    public string ReleaseNotes { get; set; } = "";
    public DateTime PublishedAt { get; set; }
}

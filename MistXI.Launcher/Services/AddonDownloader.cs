using System.IO.Compression;
using System.Net.Http;

namespace MistXI.Launcher.Services;

/// <summary>
/// Downloads and installs recommended addons and plugins from GitHub
/// </summary>
public sealed class AddonDownloader
{
    private readonly HttpClient _http;
    private readonly Logger _logger;

    public AddonDownloader(Logger logger)
    {
        _logger = logger;
        _http = new HttpClient();
        _http.DefaultRequestHeaders.UserAgent.ParseAdd("MistXI-Launcher/1.4.2");
        _http.Timeout = TimeSpan.FromMinutes(5);
    }

    /// <summary>
    /// Downloads and installs recommended addons and plugins
    /// </summary>
    public async Task EnsureRecommendedAddonsAsync(string ashitaDir, IProgress<string>? progress = null, CancellationToken ct = default)
    {
        progress?.Report("Checking recommended addons...");

        // Check and install addons
        // Note: Deeps plugin removed - requires Visual C++ runtime dependencies
        // Users should install manually from: https://github.com/relliko/Deeps/releases
        await InstallStatusTimersAsync(ashitaDir, progress, ct);
        await InstallMobDbAsync(ashitaDir, progress, ct);
    }


    private async Task InstallSdkAsync(string ashitaDir, IProgress<string>? progress, CancellationToken ct)
    {
        var pluginPath = Path.Combine(ashitaDir, "plugins", "sdk.dll");
        if (File.Exists(pluginPath))
        {
            _logger.Write("SDK plugin already installed, skipping");
            return;
        }

        try
        {
            progress?.Report("Downloading SDK plugin...");
            _logger.Write("Installing SDK plugin from GitHub");

            // Get latest release from GitHub API
            var apiUrl = "https://api.github.com/repos/AshitaXI/sdk/releases/latest";
            _http.DefaultRequestHeaders.Accept.Clear();
            _http.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
            
            var releaseJson = await _http.GetStringAsync(apiUrl, ct);
            var releaseDoc = System.Text.Json.JsonDocument.Parse(releaseJson);
            
            // Find the .zip asset
            string? downloadUrl = null;
            if (releaseDoc.RootElement.TryGetProperty("assets", out var assets))
            {
                foreach (var asset in assets.EnumerateArray())
                {
                    if (asset.TryGetProperty("name", out var name) && name.GetString()?.EndsWith(".zip") == true)
                    {
                        if (asset.TryGetProperty("browser_download_url", out var url))
                        {
                            downloadUrl = url.GetString();
                            break;
                        }
                    }
                }
            }

            if (downloadUrl == null)
            {
                _logger.Write("No SDK release found, skipping");
                return;
            }

            _logger.Write($"Found SDK release: {downloadUrl}");

            var tempDir = Path.Combine(Path.GetTempPath(), "MistXI_SDK_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            var zipPath = Path.Combine(tempDir, "sdk.zip");

            try
            {
                // Download
                using (var response = await _http.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead, ct))
                {
                    response.EnsureSuccessStatusCode();
                    using var fs = new FileStream(zipPath, FileMode.Create, FileAccess.Write, FileShare.None);
                    await response.Content.CopyToAsync(fs, ct);
                }

                // Extract
                var extractPath = Path.Combine(tempDir, "extracted");
                ZipFile.ExtractToDirectory(zipPath, extractPath);
                
                _logger.Write($"Extracted SDK contents: {string.Join(", ", Directory.GetFileSystemEntries(extractPath))}");

                // The zip might contain "plugins/sdk/" or just "sdk/" folder
                // We need to copy the DLL from inside that folder to plugins root
                var sdkDirs = Directory.GetDirectories(extractPath, "sdk", SearchOption.AllDirectories);
                string? sdkFolder = null;
                
                if (sdkDirs.Length > 0)
                {
                    sdkFolder = sdkDirs[0];
                }
                else
                {
                    // Maybe it's at root
                    var rootSdk = Path.Combine(extractPath, "sdk");
                    if (Directory.Exists(rootSdk))
                        sdkFolder = rootSdk;
                }

                if (sdkFolder != null)
                {
                    _logger.Write($"Found SDK folder at: {sdkFolder}");
                    _logger.Write($"SDK folder contents: {string.Join(", ", Directory.GetFileSystemEntries(sdkFolder))}");
                    
                    // Find sdk.dll inside the sdk folder
                    var dllFiles = Directory.GetFiles(sdkFolder, "sdk.dll", SearchOption.TopDirectoryOnly);
                    if (dllFiles.Length > 0)
                    {
                        // Copy sdk.dll to plugins root
                        Directory.CreateDirectory(Path.GetDirectoryName(pluginPath)!);
                        File.Copy(dllFiles[0], pluginPath, overwrite: true);
                        _logger.Write($"Copied sdk.dll from {dllFiles[0]} to {pluginPath}");
                        
                        progress?.Report("SDK installed");
                        _logger.Write("SDK plugin installed successfully");
                    }
                    else
                    {
                        _logger.Write("sdk.dll not found inside sdk folder");
                    }
                }
                else
                {
                    _logger.Write("SDK folder not found in archive");
                }
            }
            finally
            {
                try { Directory.Delete(tempDir, true); } catch { }
            }
        }
        catch (Exception ex)
        {
            _logger.Write("Failed to install SDK plugin", ex);
            // Non-fatal, continue
        }
    }

    private async Task InstallStatusTimersAsync(string ashitaDir, IProgress<string>? progress, CancellationToken ct)
    {
        var addonPath = Path.Combine(ashitaDir, "addons", "statustimers");
        if (Directory.Exists(addonPath))
        {
            _logger.Write("StatusTimers addon already installed, skipping");
            return;
        }

        try
        {
            progress?.Report("Downloading StatusTimers addon...");
            _logger.Write("Installing StatusTimers addon from GitHub");

            // Get latest release from GitHub API
            var apiUrl = "https://api.github.com/repos/HealsCodes/statustimers/releases/latest";
            _http.DefaultRequestHeaders.Accept.Clear();
            _http.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
            
            var releaseJson = await _http.GetStringAsync(apiUrl, ct);
            var releaseDoc = System.Text.Json.JsonDocument.Parse(releaseJson);
            
            // Find the .zip asset
            string? downloadUrl = null;
            if (releaseDoc.RootElement.TryGetProperty("assets", out var assets))
            {
                foreach (var asset in assets.EnumerateArray())
                {
                    if (asset.TryGetProperty("name", out var name) && name.GetString()?.EndsWith(".zip") == true)
                    {
                        if (asset.TryGetProperty("browser_download_url", out var url))
                        {
                            downloadUrl = url.GetString();
                            break;
                        }
                    }
                }
            }

            if (downloadUrl == null)
            {
                throw new Exception("No .zip asset found in latest release");
            }

            _logger.Write($"Found StatusTimers release: {downloadUrl}");

            // Download and extract (the zip contains "addons/statustimers/" structure)
            var tempDir = Path.Combine(Path.GetTempPath(), "MistXI_StatusTimers_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            var zipPath = Path.Combine(tempDir, "statustimers.zip");

            try
            {
                // Download
                using (var response = await _http.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead, ct))
                {
                    response.EnsureSuccessStatusCode();
                    using var fs = new FileStream(zipPath, FileMode.Create, FileAccess.Write, FileShare.None);
                    await response.Content.CopyToAsync(fs, ct);
                }

                // Extract
                var extractPath = Path.Combine(tempDir, "extracted");
                ZipFile.ExtractToDirectory(zipPath, extractPath);

                // Find the statustimers folder (inside addons/statustimers/)
                var sourcePath = Path.Combine(extractPath, "addons", "statustimers");
                if (!Directory.Exists(sourcePath))
                {
                    // Maybe it's just statustimers/ at root
                    sourcePath = Path.Combine(extractPath, "statustimers");
                }

                if (!Directory.Exists(sourcePath))
                {
                    throw new DirectoryNotFoundException($"Could not find statustimers folder in archive");
                }

                // Copy to Ashita addons folder
                CopyDirectory(sourcePath, addonPath);

                progress?.Report("StatusTimers installed");
                _logger.Write("StatusTimers addon installed successfully");
            }
            finally
            {
                // Cleanup
                try { Directory.Delete(tempDir, true); } catch { }
            }
        }
        catch (Exception ex)
        {
            _logger.Write("Failed to install StatusTimers addon", ex);
            // Non-fatal, continue
        }
    }

    private async Task InstallMobDbAsync(string ashitaDir, IProgress<string>? progress, CancellationToken ct)
    {
        var addonPath = Path.Combine(ashitaDir, "addons", "mobdb");
        if (Directory.Exists(addonPath))
        {
            _logger.Write("MobDB addon already installed, skipping");
            return;
        }

        try
        {
            progress?.Report("Downloading MobDB addon...");
            _logger.Write("Installing MobDB addon from GitHub");

            // Get latest release from GitHub API
            var apiUrl = "https://api.github.com/repos/ThornyFFXI/mobdb/releases/latest";
            _http.DefaultRequestHeaders.Accept.Clear();
            _http.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
            
            var releaseJson = await _http.GetStringAsync(apiUrl, ct);
            var releaseDoc = System.Text.Json.JsonDocument.Parse(releaseJson);
            
            // Find the .zip asset
            string? downloadUrl = null;
            if (releaseDoc.RootElement.TryGetProperty("assets", out var assets))
            {
                foreach (var asset in assets.EnumerateArray())
                {
                    if (asset.TryGetProperty("name", out var name) && name.GetString()?.EndsWith(".zip") == true)
                    {
                        if (asset.TryGetProperty("browser_download_url", out var url))
                        {
                            downloadUrl = url.GetString();
                            break;
                        }
                    }
                }
            }

            if (downloadUrl == null)
            {
                throw new Exception("No .zip asset found in latest release");
            }

            _logger.Write($"Found MobDB release: {downloadUrl}");

            // Download and extract (the zip contains "addons/mobdb/" structure)
            var tempDir = Path.Combine(Path.GetTempPath(), "MistXI_MobDB_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            var zipPath = Path.Combine(tempDir, "mobdb.zip");

            try
            {
                // Download
                using (var response = await _http.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead, ct))
                {
                    response.EnsureSuccessStatusCode();
                    using var fs = new FileStream(zipPath, FileMode.Create, FileAccess.Write, FileShare.None);
                    await response.Content.CopyToAsync(fs, ct);
                }

                // Extract
                var extractPath = Path.Combine(tempDir, "extracted");
                ZipFile.ExtractToDirectory(zipPath, extractPath);

                // Find the mobdb folder (inside addons/mobdb/)
                var sourcePath = Path.Combine(extractPath, "addons", "mobdb");
                if (!Directory.Exists(sourcePath))
                {
                    // Maybe it's just mobdb/ at root
                    sourcePath = Path.Combine(extractPath, "mobdb");
                }

                if (!Directory.Exists(sourcePath))
                {
                    throw new DirectoryNotFoundException($"Could not find mobdb folder in archive");
                }

                // Copy to Ashita addons folder
                CopyDirectory(sourcePath, addonPath);

                progress?.Report("MobDB installed");
                _logger.Write("MobDB addon installed successfully");
            }
            finally
            {
                // Cleanup
                try { Directory.Delete(tempDir, true); } catch { }
            }
        }
        catch (Exception ex)
        {
            _logger.Write("Failed to install MobDB addon", ex);
            // Non-fatal, continue
        }
    }

    private async Task InstallDeepsAsync(string ashitaDir, IProgress<string>? progress, CancellationToken ct)
    {
        var pluginPath = Path.Combine(ashitaDir, "plugins", "Deeps.dll");
        var resourcesPath = Path.Combine(ashitaDir, "resources");
        
        // Check if both DLL and resources exist
        bool dllExists = File.Exists(pluginPath);
        bool resourcesExist = Directory.Exists(resourcesPath) && Directory.GetFiles(resourcesPath, "*", SearchOption.AllDirectories).Length > 0;
        
        if (dllExists && resourcesExist)
        {
            _logger.Write("Deeps plugin and resources already installed, skipping");
            return;
        }

        try
        {
            progress?.Report("Downloading Deeps plugin...");
            _logger.Write("Installing Deeps plugin from GitHub");
            _logger.Write($"DLL exists: {dllExists}, Resources exist: {resourcesExist}");

            // Get latest release from GitHub API
            var apiUrl = "https://api.github.com/repos/relliko/Deeps/releases/latest";
            _http.DefaultRequestHeaders.Accept.Clear();
            _http.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
            
            var releaseJson = await _http.GetStringAsync(apiUrl, ct);
            var releaseDoc = System.Text.Json.JsonDocument.Parse(releaseJson);
            
            // Find the .zip asset (should be something like Deeps1.06_i4.16.zip)
            string? downloadUrl = null;
            if (releaseDoc.RootElement.TryGetProperty("assets", out var assets))
            {
                foreach (var asset in assets.EnumerateArray())
                {
                    if (asset.TryGetProperty("name", out var name))
                    {
                        var fileName = name.GetString();
                        // Look for the i4.16 or i4 version (Ashita v4)
                        if (fileName?.Contains("i4") == true && fileName.EndsWith(".zip"))
                        {
                            if (asset.TryGetProperty("browser_download_url", out var url))
                            {
                                downloadUrl = url.GetString();
                                _logger.Write($"Found Deeps release: {fileName}");
                                break;
                            }
                        }
                    }
                }
            }

            if (downloadUrl == null)
            {
                // Fallback to direct v1.06 link
                downloadUrl = "https://github.com/relliko/Deeps/releases/download/v1.06/Deeps1.06_i4.16.zip";
                _logger.Write("Using fallback URL: " + downloadUrl);
            }
            
            var tempDir = Path.Combine(Path.GetTempPath(), "MistXI_Deeps_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            var zipPath = Path.Combine(tempDir, "deeps.zip");

            _logger.Write($"Downloading Deeps from: {downloadUrl}");
            _logger.Write($"Temp directory: {tempDir}");

            // Download zip
            using (var response = await _http.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead, ct))
            {
                response.EnsureSuccessStatusCode();
                using var fs = new FileStream(zipPath, FileMode.Create, FileAccess.Write, FileShare.None);
                await response.Content.CopyToAsync(fs, ct);
            }

            _logger.Write($"Downloaded {new FileInfo(zipPath).Length} bytes");

            // Extract the entire archive
            var extractPath = Path.Combine(tempDir, "extracted");
            ZipFile.ExtractToDirectory(zipPath, extractPath);
            
            _logger.Write($"Extracted to: {extractPath}");
            _logger.Write($"Contents: {string.Join(", ", Directory.GetFileSystemEntries(extractPath))}");

            // Find and copy Deeps.dll to plugins folder
            var dllFiles = Directory.GetFiles(extractPath, "Deeps.dll", SearchOption.AllDirectories);
            _logger.Write($"Found {dllFiles.Length} DLL files");
            
            if (dllFiles.Length == 0)
            {
                throw new FileNotFoundException("Deeps.dll not found in downloaded archive");
            }

            // If multiple DLLs, prefer one NOT in a "x64" folder (Ashita v4 is 32-bit)
            var selectedDll = dllFiles[0];
            if (dllFiles.Length > 1)
            {
                var nonX64 = dllFiles.FirstOrDefault(d => !d.Contains("x64", StringComparison.OrdinalIgnoreCase));
                if (nonX64 != null)
                {
                    selectedDll = nonX64;
                    _logger.Write($"Selected 32-bit DLL: {selectedDll}");
                }
            }

            // Copy DLL to plugins folder
            Directory.CreateDirectory(Path.GetDirectoryName(pluginPath)!);
            File.Copy(selectedDll, pluginPath, overwrite: true);
            _logger.Write($"Copied Deeps.dll from {selectedDll} to {pluginPath}");

            // Find and copy resources folder to Ashita root
            var allDirs = Directory.GetDirectories(extractPath, "*", SearchOption.AllDirectories);
            _logger.Write($"All directories in archive: {string.Join(", ", allDirs.Select(d => Path.GetFileName(d)))}");
            
            var resourcesDirs = Directory.GetDirectories(extractPath, "resources", SearchOption.AllDirectories);
            _logger.Write($"Found {resourcesDirs.Length} resources directories");
            
            string? sourceResourcesPath = null;
            if (resourcesDirs.Length > 0)
            {
                sourceResourcesPath = resourcesDirs[0];
            }
            else
            {
                // Check root level
                var rootResources = Path.Combine(extractPath, "resources");
                if (Directory.Exists(rootResources))
                {
                    sourceResourcesPath = rootResources;
                }
            }

            if (sourceResourcesPath != null && Directory.Exists(sourceResourcesPath))
            {
                _logger.Write($"Found resources at: {sourceResourcesPath}");
                _logger.Write($"Resources contents: {string.Join(", ", Directory.GetFileSystemEntries(sourceResourcesPath))}");
                
                // Copy resources folder to Ashita root
                CopyDirectory(sourceResourcesPath, resourcesPath);
                _logger.Write($"Copied resources to {resourcesPath}");
                
                var copiedFiles = Directory.GetFiles(resourcesPath, "*", SearchOption.AllDirectories);
                _logger.Write($"Copied {copiedFiles.Length} resource files");
            }
            else
            {
                _logger.Write("ERROR: resources folder not found in Deeps archive!");
                _logger.Write($"Searched in: {extractPath}");
            }

            // Cleanup
            try { Directory.Delete(tempDir, true); } catch { }

            progress?.Report("Deeps installed");
            _logger.Write("Deeps plugin installation complete");
        }
        catch (Exception ex)
        {
            _logger.Write("Failed to install Deeps plugin", ex);
            // Non-fatal, continue
        }
    }

    private void CopyDirectory(string sourceDir, string destDir)
    {
        // Create destination directory
        Directory.CreateDirectory(destDir);

        // Copy all files
        foreach (var file in Directory.GetFiles(sourceDir))
        {
            var destFile = Path.Combine(destDir, Path.GetFileName(file));
            File.Copy(file, destFile, overwrite: true);
        }

        // Copy all subdirectories recursively
        foreach (var subDir in Directory.GetDirectories(sourceDir))
        {
            var destSubDir = Path.Combine(destDir, Path.GetFileName(subDir));
            CopyDirectory(subDir, destSubDir);
        }
    }
}

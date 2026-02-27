using System.Text.Json;

namespace MistXI.Launcher.Services;

public sealed class VersionService
{
    private readonly HttpClient _http;
    private readonly Logger _logger;

    public VersionService(Logger logger)
    {
        _logger = logger;
        _http = new HttpClient();
        _http.DefaultRequestHeaders.UserAgent.ParseAdd("MistXI-Launcher/1.5");
        _http.Timeout = TimeSpan.FromSeconds(10);
    }

    /// <summary>
    /// Gets the current required client version from the server
    /// </summary>
    public async Task<string?> GetServerVersionAsync()
    {
        try
        {
            var response = await _http.GetStringAsync("https://api.mistxi.com/status.php");
            var doc = JsonDocument.Parse(response);
            
            if (doc.RootElement.TryGetProperty("clientVersion", out var versionElement))
            {
                var version = versionElement.GetString();
                _logger.Write($"Server version: {version}");
                return version;
            }
            
            _logger.Write("clientVersion not found in server response");
            return null;
        }
        catch (Exception ex)
        {
            _logger.Write("Failed to get server version", ex);
            return null;
        }
    }

    /// <summary>
    /// Gets the local client version by checking multiple sources
    /// </summary>
    public string? GetLocalClientVersion(string ffxiPath)
    {
        // Try multiple methods in order of reliability
        string? version = null;

        // Method 1: Check patch2.cfg (MOST RELIABLE - used by PlayOnline)
        version = TryReadVersionFromPatchCfg(ffxiPath);
        if (version != null)
        {
            _logger.Write($"Local version from patch2.cfg: {version}");
            return version;
        }

        // Method 2: Check ROM/0/FTABLE.DAT (fallback)
        version = TryReadVersionFromFTable(ffxiPath);
        if (version != null)
        {
            _logger.Write($"Local version from FTABLE.DAT: {version}");
            return version;
        }

        // Method 3: Check for VER.dat or similar version files
        version = TryReadVersionFromVerDat(ffxiPath);
        if (version != null)
        {
            _logger.Write($"Local version from VER.dat: {version}");
            return version;
        }

        // Method 4: Read from Windows Registry
        version = TryReadVersionFromRegistry();
        if (version != null)
        {
            _logger.Write($"Local version from Registry: {version}");
            return version;
        }

        _logger.Write("Could not detect local client version from any source");
        return null;
    }

    private string? TryReadVersionFromPatchCfg(string ffxiPath)
    {
        try
        {
            var patchCfgPath = Path.Combine(ffxiPath, "patch2.cfg");
            
            if (!File.Exists(patchCfgPath))
            {
                _logger.Write("patch2.cfg not found");
                return null;
            }

            _logger.Write("Reading patch2.cfg...");
            var content = File.ReadAllText(patchCfgPath);
            
            // Find all version numbers in the file (format: YYYYMMDD_N)
            var matches = System.Text.RegularExpressions.Regex.Matches(content, @"(\d{8}_\d)");
            
            if (matches.Count == 0)
            {
                _logger.Write("No version patterns found in patch2.cfg");
                return null;
            }

            // Find the highest (most recent) version number
            string? latestVersion = null;
            foreach (System.Text.RegularExpressions.Match match in matches)
            {
                var version = match.Groups[1].Value;
                if (latestVersion == null || string.Compare(version, latestVersion) > 0)
                {
                    latestVersion = version;
                }
            }

            _logger.Write($"Found {matches.Count} version entries in patch2.cfg, latest: {latestVersion}");
            return latestVersion;
        }
        catch (Exception ex)
        {
            _logger.Write("Failed to read patch2.cfg", ex);
            return null;
        }
    }

    private string? TryReadVersionFromFTable(string ffxiPath)
    {
        try
        {
            var ftablePath = Path.Combine(ffxiPath, "ROM", "0", "FTABLE.DAT");
            
            if (!File.Exists(ftablePath))
            {
                _logger.Write("FTABLE.DAT not found");
                return null;
            }

            // Read file and search for version pattern (e.g., "30260203_0")
            var content = File.ReadAllBytes(ftablePath);
            var text = System.Text.Encoding.ASCII.GetString(content);
            
            // Match version pattern: 8 digits, underscore, 1 digit
            var matches = System.Text.RegularExpressions.Regex.Matches(text, @"(\d{8}_\d)");
            
            // Find the most recent-looking version (highest number)
            string? bestMatch = null;
            foreach (System.Text.RegularExpressions.Match match in matches)
            {
                var candidate = match.Groups[1].Value;
                if (bestMatch == null || string.Compare(candidate, bestMatch) > 0)
                {
                    bestMatch = candidate;
                }
            }
            
            return bestMatch;
        }
        catch (Exception ex)
        {
            _logger.Write("Failed to read FTABLE.DAT", ex);
            return null;
        }
    }

    private string? TryReadVersionFromVerDat(string ffxiPath)
    {
        try
        {
            // Check ROM/0/VER.dat or similar
            var verPaths = new[]
            {
                Path.Combine(ffxiPath, "ROM", "0", "VER.dat"),
                Path.Combine(ffxiPath, "ROM", "VER.dat"),
                Path.Combine(ffxiPath, "VER.dat")
            };

            foreach (var verPath in verPaths)
            {
                if (!File.Exists(verPath))
                    continue;

                var content = File.ReadAllText(verPath).Trim();
                
                // Check if it matches version pattern
                if (System.Text.RegularExpressions.Regex.IsMatch(content, @"^\d{8}_\d$"))
                {
                    return content;
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.Write("Failed to read VER.dat", ex);
            return null;
        }
    }

    private string? TryReadVersionFromRegistry()
    {
        try
        {
            // Check FFXI registry keys for version info
            using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\PlayOnlineUS\InstallFolder");
            if (key != null)
            {
                var version = key.GetValue("Version") as string;
                if (!string.IsNullOrWhiteSpace(version) && 
                    System.Text.RegularExpressions.Regex.IsMatch(version, @"\d{8}_\d"))
                {
                    return version;
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.Write("Failed to read version from registry", ex);
            return null;
        }
    }

    /// <summary>
    /// Checks if there's a version mismatch between local client and server
    /// Returns: null (unknown), true (mismatch), false (match)
    /// </summary>
    public async Task<bool?> CheckVersionMismatchAsync(string ffxiPath)
    {
        _logger.Write("=== Version Check Starting ===");
        
        var serverVersion = await GetServerVersionAsync();
        if (serverVersion == null)
        {
            _logger.Write("❌ Cannot check version - server version unavailable");
            return null; // Can't determine
        }

        _logger.Write($"✅ Server version: {serverVersion}");

        var localVersion = GetLocalClientVersion(ffxiPath);
        if (localVersion == null)
        {
            _logger.Write("⚠️ Cannot check version - local version undetectable");
            _logger.Write($"FFXI Path checked: {ffxiPath}");
            _logger.Write("This may indicate the client needs initial setup or POL file check");
            return null; // Can't determine
        }

        _logger.Write($"✅ Local version: {localVersion}");

        var mismatch = serverVersion != localVersion;
        
        if (mismatch)
        {
            _logger.Write($"🚨 VERSION MISMATCH DETECTED!");
            _logger.Write($"   Server expects: {serverVersion}");
            _logger.Write($"   Client has:     {localVersion}");
            _logger.Write($"   User will need to update client");
        }
        else
        {
            _logger.Write($"✅ Version OK - Client matches server");
        }

        _logger.Write("=== Version Check Complete ===");
        return mismatch;
    }

    /// <summary>
    /// Monitors the game launch for version mismatch errors
    /// </summary>
    public async Task<bool> MonitorForVersionErrorAsync(CancellationToken ct = default)
    {
        try
        {
            // Wait for game to initialize
            await Task.Delay(8000, ct);

            // Check if POL error window appeared
            var polProcesses = System.Diagnostics.Process.GetProcessesByName("pol");
            
            if (polProcesses.Length > 0)
            {
                // POL is running - might be showing error
                // Check if Ashita/game actually started
                var ashitaProcesses = System.Diagnostics.Process.GetProcessesByName("Ashita-cli");
                var ffxiProcesses = System.Diagnostics.Process.GetProcessesByName("pol");

                if (ashitaProcesses.Length == 0)
                {
                    // Game didn't start but POL is running - likely error
                    _logger.Write("Possible version error detected (POL running, game not started)");
                    return true;
                }
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.Write("Error monitoring for version mismatch", ex);
            return false;
        }
    }
}

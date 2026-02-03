using System.Diagnostics;

namespace MistXI.Launcher.Services;

public sealed class PlayOnlineService
{
    private readonly Logger _logger;
    private readonly HttpClient _http;

    public PlayOnlineService(Logger logger)
    {
        _logger = logger;
        _http = new HttpClient();
        _http.DefaultRequestHeaders.UserAgent.ParseAdd("MistXI-Launcher/0.1");
        _http.Timeout = TimeSpan.FromMinutes(10);
    }

    /// <summary>
    /// Downloads the DSP patch from MistXI website
    /// </summary>
    public async Task<string> DownloadDspPatchAsync(IProgress<string>? progress = null, CancellationToken ct = default)
    {
        progress?.Report("Downloading DSP patch...");
        
        var tempDir = Path.Combine(Path.GetTempPath(), "MistXI_DSPPatch_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        
        var zipPath = Path.Combine(tempDir, "FFXI-UpdatePatch.zip");
        var url = "https://mistxi.com/FFXI-UpdatePatch.zip";

        try
        {
            using var response = await _http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
            response.EnsureSuccessStatusCode();

            using var fs = new FileStream(zipPath, FileMode.Create, FileAccess.Write, FileShare.None);
            await response.Content.CopyToAsync(fs, ct);

            _logger.Write($"DSP patch downloaded to: {zipPath}");
            progress?.Report("DSP patch downloaded");
            
            return zipPath;
        }
        catch (Exception ex)
        {
            _logger.Write("Failed to download DSP patch", ex);
            
            // Check if it's a network/connectivity issue
            if (ex is HttpRequestException || ex.Message.Contains("network") || ex.Message.Contains("connection"))
            {
                throw new InvalidOperationException(
                    "Failed to download DSP patch from MistXI website.\n\n" +
                    "Possible causes:\n" +
                    "• No internet connection\n" +
                    "• Firewall or antivirus blocking the download\n" +
                    "• MistXI website is temporarily unavailable\n\n" +
                    "Manual workaround:\n" +
                    "1. Download the patch manually from: https://mistxi.com/FFXI-UpdatePatch.zip\n" +
                    "2. Extract it to your FFXI folder\n" +
                    $"3. Or contact support on Discord\n\nError: {ex.Message}", 
                    ex);
            }
            
            throw new InvalidOperationException($"Failed to download DSP patch from MistXI website: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Installs DSP patch to FFXI directory using elevated helper
    /// </summary>
    public async Task InstallDspPatchAsync(string zipPath, string ffxiPath, IProgress<string>? progress = null)
    {
        progress?.Report("Installing DSP patch...");
        
        try
        {
            // Normalize and validate paths - remove trailing backslashes
            zipPath = Path.GetFullPath(zipPath).TrimEnd('\\', '/');
            ffxiPath = Path.GetFullPath(ffxiPath).TrimEnd('\\', '/');
            
            _logger.Write($"DSP Patch - Zip path: {zipPath}");
            _logger.Write($"DSP Patch - FFXI path: {ffxiPath}");
            
            if (!File.Exists(zipPath))
            {
                _logger.Write($"ERROR: Zip file not found at: {zipPath}");
                throw new FileNotFoundException($"DSP patch zip file not found: {zipPath}");
            }

            if (!Directory.Exists(ffxiPath))
            {
                _logger.Write($"ERROR: FFXI directory not found at: {ffxiPath}");
                throw new DirectoryNotFoundException($"FFXI directory not found: {ffxiPath}");
            }

            // Extract the embedded helper executable
            var helperPath = Path.Combine(Path.GetTempPath(), "MistXI.PatchHelper.exe");
            _logger.Write($"Extracting helper to: {helperPath}");
            
            using (var resourceStream = typeof(PlayOnlineService).Assembly.GetManifestResourceStream("MistXI.PatchHelper.exe"))
            {
                if (resourceStream == null)
                {
                    throw new InvalidOperationException("Patch helper executable not found in launcher resources");
                }

                using var fileStream = new FileStream(helperPath, FileMode.Create, FileAccess.Write);
                await resourceStream.CopyToAsync(fileStream);
            }

            _logger.Write($"Helper extracted successfully");
            progress?.Report("Requesting administrator privileges...");

            // Run the helper with elevation
            var psi = new ProcessStartInfo
            {
                FileName = helperPath,
                Arguments = $"dsp \"{zipPath}\" \"{ffxiPath}\"",
                UseShellExecute = true,
                Verb = "runas", // Request elevation
                CreateNoWindow = false,
                WindowStyle = ProcessWindowStyle.Hidden
            };

            _logger.Write($"Launching helper with args: {psi.Arguments}");
            using var process = Process.Start(psi);
            
            if (process == null)
            {
                throw new InvalidOperationException("Failed to start patch helper");
            }

            await Task.Run(() => process.WaitForExit());
            _logger.Write($"Helper exited with code: {process.ExitCode}");

            if (process.ExitCode != 0)
            {
                var errorMsg = process.ExitCode switch
                {
                    1 => "Invalid arguments passed to helper",
                    2 => "Zip file not found",
                    3 => "FFXI directory not found",
                    99 => "Unexpected error during installation",
                    _ => $"Unknown error (exit code {process.ExitCode})"
                };
                throw new InvalidOperationException($"Patch helper failed: {errorMsg}");
            }

            // Cleanup helper
            try
            {
                File.Delete(helperPath);
                _logger.Write("Cleaned up helper executable");
            }
            catch
            {
                // Non-critical
            }

            progress?.Report("DSP patch installed successfully");
            _logger.Write("DSP patch installation complete");
        }
        catch (System.ComponentModel.Win32Exception ex) when (ex.NativeErrorCode == 1223)
        {
            // User cancelled UAC prompt
            _logger.Write("User cancelled elevation prompt");
            throw new OperationCanceledException("Administrator privileges required but elevation was cancelled by user");
        }
        catch (Exception ex)
        {
            _logger.Write("Failed to install DSP patch", ex);
            throw;
        }
    }

    /// <summary>
    /// Checks if DSP patch is needed (FFXI doesn't show in POL Check Files)
    /// </summary>
    public bool IsDspPatchNeeded(string ffxiPath)
    {
        // The DSP patch is needed if FFXI was installed via the official installer
        // Check for specific files that indicate DSP patch has been applied
        
        // Check for the DSP marker file (if we create one after patching)
        var dspMarker = Path.Combine(ffxiPath, ".mistxi_dsp_applied");
        if (File.Exists(dspMarker))
        {
            _logger.Write("DSP patch already applied (marker found)");
            return false;
        }

        // If the marker doesn't exist but ROM structure is present, patch is needed
        var romPath = Path.Combine(ffxiPath, "ROM");
        if (Directory.Exists(romPath))
        {
            _logger.Write("DSP patch appears to be needed (no marker file)");
            return true;
        }

        // If ROM doesn't exist at all, something is wrong
        _logger.Write("Warning: ROM folder not found, cannot determine DSP patch status");
        return false;
    }

    /// <summary>
    /// Copies data folder from PlayOnline Viewer to FFXI directory using elevated helper
    /// </summary>
    public async Task CopyDataFolderAsync(string polViewerPath, string ffxiPath, IProgress<int>? progress = null, CancellationToken ct = default)
    {
        var sourceDataPath = Path.Combine(polViewerPath, "data").TrimEnd('\\', '/');
        var destDataPath = Path.Combine(ffxiPath, "data").TrimEnd('\\', '/');

        if (!Directory.Exists(sourceDataPath))
        {
            throw new DirectoryNotFoundException($"PlayOnline Viewer data folder not found: {sourceDataPath}");
        }

        _logger.Write($"Starting data folder copy from {sourceDataPath} to {destDataPath}");
        progress?.Report(0);

        try
        {
            // Count files for progress
            var files = Directory.GetFiles(sourceDataPath, "*", SearchOption.AllDirectories);
            var totalBytes = files.Sum(f => new FileInfo(f).Length);
            _logger.Write($"Found {files.Length} files totaling {totalBytes / 1024 / 1024} MB");

            // Extract the embedded helper executable
            var helperPath = Path.Combine(Path.GetTempPath(), "MistXI.PatchHelper.exe");
            
            using (var resourceStream = typeof(PlayOnlineService).Assembly.GetManifestResourceStream("MistXI.PatchHelper.exe"))
            {
                if (resourceStream == null)
                {
                    throw new InvalidOperationException("Patch helper executable not found in launcher resources");
                }

                using var fileStream = new FileStream(helperPath, FileMode.Create, FileAccess.Write);
                await resourceStream.CopyToAsync(fileStream, ct);
            }

            _logger.Write($"Extracted helper to: {helperPath}");
            progress?.Report(10);

            // Run the helper with elevation
            var psi = new ProcessStartInfo
            {
                FileName = helperPath,
                Arguments = $"copy \"{sourceDataPath}\" \"{destDataPath}\"",
                UseShellExecute = true,
                Verb = "runas", // Request elevation
                CreateNoWindow = false,
                WindowStyle = ProcessWindowStyle.Hidden
            };

            _logger.Write($"Launching helper with args: {psi.Arguments}");
            using var process = Process.Start(psi);
            
            if (process == null)
            {
                throw new InvalidOperationException("Failed to start copy helper");
            }

            // Wait with progress updates
            progress?.Report(20);
            var startTime = DateTime.UtcNow;
            
            while (!process.HasExited)
            {
                await Task.Delay(500, ct);
                
                // Estimate progress based on time (very rough)
                var elapsed = (DateTime.UtcNow - startTime).TotalSeconds;
                var estimatedTotal = 30.0; // Rough estimate: 30 seconds
                var percentEstimate = Math.Min(90, 20 + (int)(elapsed / estimatedTotal * 70));
                progress?.Report(percentEstimate);
            }

            _logger.Write($"Helper exited with code: {process.ExitCode}");

            if (process.ExitCode != 0)
            {
                var errorMsg = process.ExitCode switch
                {
                    1 => "Invalid arguments passed to helper",
                    2 => "Source directory not found",
                    3 => "FFXI directory not found",
                    4 => "Source directory is empty",
                    99 => "Unexpected error during copy",
                    _ => $"Unknown error (exit code {process.ExitCode})"
                };
                throw new InvalidOperationException($"Data copy helper failed: {errorMsg}");
            }

            // Cleanup helper
            try
            {
                File.Delete(helperPath);
                _logger.Write("Cleaned up helper executable");
            }
            catch
            {
                // Non-critical
            }

            _logger.Write("Data folder copy completed successfully");
            progress?.Report(100);
        }
        catch (System.ComponentModel.Win32Exception ex) when (ex.NativeErrorCode == 1223)
        {
            // User cancelled UAC prompt
            _logger.Write("User cancelled elevation prompt for data copy");
            throw new OperationCanceledException("Administrator privileges required but elevation was cancelled by user");
        }
        catch (Exception ex)
        {
            _logger.Write("Data folder copy failed", ex);
            throw new InvalidOperationException("Failed to copy data folder", ex);
        }
    }

    /// <summary>
    /// Launches PlayOnline Viewer for file check
    /// </summary>
    public void LaunchPlayOnlineViewer(string polViewerPath)
    {
        var polExe = Path.Combine(polViewerPath, "pol.exe");
        
        if (!File.Exists(polExe))
        {
            throw new FileNotFoundException($"PlayOnline Viewer executable not found: {polExe}");
        }

        _logger.Write($"Launching PlayOnline Viewer: {polExe}");
        
        Process.Start(new ProcessStartInfo
        {
            FileName = polExe,
            WorkingDirectory = polViewerPath,
            UseShellExecute = true
        });
    }

    /// <summary>
    /// Checks if PlayOnline Viewer is currently running
    /// </summary>
    public bool IsPlayOnlineRunning()
    {
        return Process.GetProcessesByName("pol").Any() || 
               Process.GetProcessesByName("PlayOnlineViewer").Any();
    }

    /// <summary>
    /// Triggers a client update by deleting the 0.dat file
    /// </summary>
    public void TriggerClientUpdate(string ffxiPath)
    {
        var datPath = Path.Combine(ffxiPath, "ROM", "0", "0.dat");
        
        if (File.Exists(datPath))
        {
            _logger.Write($"Deleting 0.dat to trigger client update: {datPath}");
            File.Delete(datPath);
            _logger.Write("0.dat deleted successfully - client update will trigger on next POL file check");
        }
        else
        {
            _logger.Write("0.dat not found - may already be deleted or client needs initial setup");
        }
    }

    /// <summary>
    /// Validates that data folder was copied successfully
    /// </summary>
    public bool ValidateDataFolderCopy(string ffxiPath)
    {
        var dataPath = Path.Combine(ffxiPath, "data");
        
        if (!Directory.Exists(dataPath))
            return false;

        // Check for some expected files
        var expectedFiles = new[]
        {
            Path.Combine(dataPath, "vtable.dat"),
            Path.Combine(dataPath, "menu.dat")
        };

        return expectedFiles.All(File.Exists);
    }

}

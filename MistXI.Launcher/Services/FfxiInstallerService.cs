using System.Diagnostics;
using System.IO;
using System.Net.Http;

namespace MistXI.Launcher.Services;

/// <summary>
/// Handles downloading and extracting the official FFXI installer from Square Enix
/// </summary>
public sealed class FfxiInstallerService
{
    private readonly HttpClient _http;
    private readonly Logger _log;

    private static readonly string[] InstallerUrls = new[]
    {
        "https://gdl.square-enix.com/ffxi/download/us/FFXIFullSetup_US.part1.exe",
        "https://gdl.square-enix.com/ffxi/download/us/FFXIFullSetup_US.part2.rar",
        "https://gdl.square-enix.com/ffxi/download/us/FFXIFullSetup_US.part3.rar",
        "https://gdl.square-enix.com/ffxi/download/us/FFXIFullSetup_US.part4.rar",
        "https://gdl.square-enix.com/ffxi/download/us/FFXIFullSetup_US.part5.rar"
    };

    public FfxiInstallerService(HttpClient http, Logger log)
    {
        _http = http;
        _log = log;
    }

    /// <summary>
    /// Downloads all 5 parts of the FFXI installer to the specified directory
    /// </summary>
    public async Task DownloadInstallerAsync(string destDir, IProgress<(int fileIndex, int totalFiles, long bytesDownloaded, long? totalBytes, string status)>? progress = null, CancellationToken ct = default)
    {
        Directory.CreateDirectory(destDir);

        for (int i = 0; i < InstallerUrls.Length; i++)
        {
            var url = InstallerUrls[i];
            var fileName = Path.GetFileName(url);
            var destPath = Path.Combine(destDir, fileName);

            // Skip if already downloaded
            if (File.Exists(destPath))
            {
                var fileInfo = new FileInfo(destPath);
                _log.Write($"Installer part {i + 1} already exists ({fileInfo.Length:N0} bytes), skipping download");
                progress?.Report((i + 1, InstallerUrls.Length, fileInfo.Length, fileInfo.Length, $"Part {i + 1}/5 already downloaded"));
                continue;
            }

            _log.Write($"Downloading installer part {i + 1}/5: {fileName}");
            progress?.Report((i + 1, InstallerUrls.Length, 0, null, $"Downloading part {i + 1}/5..."));

            try
            {
                using var response = await _http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
                response.EnsureSuccessStatusCode();

                var totalBytes = response.Content.Headers.ContentLength;
                using var contentStream = await response.Content.ReadAsStreamAsync(ct);
                using var fileStream = new FileStream(destPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);

                var buffer = new byte[8192];
                long totalBytesRead = 0;
                int bytesRead;

                while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length, ct)) > 0)
                {
                    await fileStream.WriteAsync(buffer, 0, bytesRead, ct);
                    totalBytesRead += bytesRead;
                    progress?.Report((i + 1, InstallerUrls.Length, totalBytesRead, totalBytes, $"Part {i + 1}/5: {totalBytesRead:N0} bytes"));
                }

                _log.Write($"Downloaded part {i + 1}/5: {fileName} ({totalBytesRead:N0} bytes)");
            }
            catch (Exception ex)
            {
                _log.Write($"Failed to download part {i + 1}/5: {fileName}", ex);
                throw new InvalidOperationException($"Failed to download installer part {i + 1}: {ex.Message}", ex);
            }
        }
    }

    /// <summary>
    /// Extracts the installer by running part1.exe which unpacks all RAR files
    /// </summary>
    public async Task<string> ExtractInstallerAsync(string installerDir, IProgress<string>? progress = null, CancellationToken ct = default)
    {
        var part1Path = Path.Combine(installerDir, "FFXIFullSetup_US.part1.exe");
        
        if (!File.Exists(part1Path))
        {
            throw new FileNotFoundException("Installer part1.exe not found. Download may have failed.", part1Path);
        }

        var extractedDir = Path.Combine(installerDir, "FFXIFullSetup_US");
        
        // Check if already extracted
        var setupExe = Path.Combine(extractedDir, "FFXISetup.exe");
        if (File.Exists(setupExe))
        {
            _log.Write("Installer already extracted");
            progress?.Report("Installer already extracted");
            return extractedDir;
        }

        _log.Write("Extracting installer files...");
        progress?.Report("Extracting installer files (this may take a few minutes)...");

        try
        {
            // Run part1.exe which will extract all the RAR files into FFXIFullSetup_US folder
            var psi = new ProcessStartInfo
            {
                FileName = part1Path,
                WorkingDirectory = installerDir,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using var process = Process.Start(psi);
            if (process == null)
            {
                throw new InvalidOperationException("Failed to start extraction process");
            }

            // Wait for extraction to complete (timeout after 10 minutes)
            var completed = await Task.Run(() => process.WaitForExit(600000), ct);
            
            if (!completed)
            {
                process.Kill();
                throw new TimeoutException("Installer extraction timed out after 10 minutes");
            }

            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException($"Installer extraction failed with exit code {process.ExitCode}");
            }

            // Verify extraction succeeded
            if (!File.Exists(setupExe))
            {
                throw new InvalidOperationException("Extraction completed but FFXISetup.exe not found");
            }

            _log.Write($"Extraction complete: {extractedDir}");
            progress?.Report("Extraction complete!");
            return extractedDir;
        }
        catch (Exception ex)
        {
            _log.Write("Installer extraction failed", ex);
            throw;
        }
    }

    /// <summary>
    /// Launches the FFXI setup wizard (user must complete manually)
    /// </summary>
    public void LaunchSetupWizard(string extractedDir)
    {
        var setupExe = Path.Combine(extractedDir, "FFXISetup.exe");
        
        if (!File.Exists(setupExe))
        {
            throw new FileNotFoundException("FFXISetup.exe not found in extracted directory", setupExe);
        }

        _log.Write("Launching FFXI setup wizard...");

        var psi = new ProcessStartInfo
        {
            FileName = setupExe,
            WorkingDirectory = extractedDir,
            UseShellExecute = true
        };

        Process.Start(psi);
    }

    /// <summary>
    /// Cleans up installer files after successful installation
    /// </summary>
    public void CleanupInstallerFiles(string installerDir)
    {
        try
        {
            _log.Write($"Cleaning up installer files in: {installerDir}");

            // Delete all the downloaded parts
            foreach (var url in InstallerUrls)
            {
                var fileName = Path.GetFileName(url);
                var filePath = Path.Combine(installerDir, fileName);
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    _log.Write($"Deleted: {fileName}");
                }
            }

            // Delete the extracted folder
            var extractedDir = Path.Combine(installerDir, "FFXIFullSetup_US");
            if (Directory.Exists(extractedDir))
            {
                Directory.Delete(extractedDir, recursive: true);
                _log.Write($"Deleted extracted folder: {extractedDir}");
            }

            // Delete the installer directory if it's empty
            if (Directory.Exists(installerDir) && !Directory.EnumerateFileSystemEntries(installerDir).Any())
            {
                Directory.Delete(installerDir);
                _log.Write($"Deleted empty installer directory");
            }

            _log.Write("Installer cleanup complete");
        }
        catch (Exception ex)
        {
            _log.Write("Failed to clean up installer files (non-critical)", ex);
            // Non-critical - don't throw
        }
    }

    /// <summary>
    /// Gets the total size of all installer parts (approximate)
    /// </summary>
    public static long GetEstimatedDownloadSize()
    {
        // Based on actual file sizes from Square Enix (~3.5 GB total)
        return 3_500_000_000L; // 3.5 GB
    }
}

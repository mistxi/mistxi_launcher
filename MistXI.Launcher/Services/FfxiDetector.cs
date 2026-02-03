using System.Diagnostics;
using Microsoft.Win32;

namespace MistXI.Launcher.Services;

public sealed class FfxiDetector
{
    public string? AutoDetectFfxiPath()
    {
        // Common installation paths to check
        var candidatePaths = new List<string>
        {
            // Registry check first
            GetPathFromRegistry(),
            
            // Common install locations - FFXI specific paths
            @"C:\Program Files (x86)\PlayOnline\SquareEnix\FINAL FANTASY XI",
            @"C:\Program Files\PlayOnline\SquareEnix\FINAL FANTASY XI",
            @"D:\Program Files (x86)\PlayOnline\SquareEnix\FINAL FANTASY XI",
            @"D:\PlayOnline\SquareEnix\FINAL FANTASY XI",
            @"C:\PlayOnline\SquareEnix\FINAL FANTASY XI",
            @"E:\Program Files (x86)\PlayOnline\SquareEnix\FINAL FANTASY XI",
            @"E:\PlayOnline\SquareEnix\FINAL FANTASY XI",
            
            // Alternative structures
            @"C:\SquareEnix\FINAL FANTASY XI",
            @"D:\SquareEnix\FINAL FANTASY XI",
            @"C:\Games\FINAL FANTASY XI",
            @"D:\Games\FINAL FANTASY XI",
            
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), @"PlayOnline\SquareEnix\FINAL FANTASY XI"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), @"PlayOnline\SquareEnix\FINAL FANTASY XI"),
        };

        // Remove nulls and check each path
        foreach (var path in candidatePaths.Where(p => !string.IsNullOrWhiteSpace(p)))
        {
            if (IsValidFfxiPath(path!))
                return path;
        }

        return null;
    }

    public bool IsValidFfxiPath(string path)
    {
        if (!Directory.Exists(path))
            return false;

        // Check for essential FFXI files
        var requiredFiles = new[]
        {
            "polboot.exe",
            "ffximain.dll"
        };

        // Both files must exist
        return requiredFiles.All(file => File.Exists(Path.Combine(path, file)));
    }

    private string? GetPathFromRegistry()
    {
        try
        {
            // Check FFXI registry keys
            var registryPaths = new[]
            {
                @"SOFTWARE\PlayOnline\SquareEnix\FinalFantasyXI",
                @"SOFTWARE\WOW6432Node\PlayOnline\SquareEnix\FinalFantasyXI",
                @"SOFTWARE\PlayOnlineUS\InstallFolder",
                @"SOFTWARE\WOW6432Node\PlayOnlineUS\InstallFolder",
                @"SOFTWARE\PlayOnlineUS\SquareEnix\FinalFantasyXI",
                @"SOFTWARE\WOW6432Node\PlayOnlineUS\SquareEnix\FinalFantasyXI"
            };

            foreach (var regPath in registryPaths)
            {
                using var key = Registry.LocalMachine.OpenSubKey(regPath);
                if (key != null)
                {
                    var installPath = key.GetValue("InstallDir") as string 
                                   ?? key.GetValue("0001") as string
                                   ?? key.GetValue("InstallFolder") as string
                                   ?? key.GetValue("Path") as string;
                    
                    if (!string.IsNullOrWhiteSpace(installPath))
                    {
                        // Registry might point to PlayOnline folder, look for FFXI subfolder
                        if (Directory.Exists(installPath) && IsValidFfxiPath(installPath))
                            return installPath;
                        
                        // Try common FFXI subpaths
                        var ffxiSubPath = Path.Combine(installPath, "SquareEnix", "FINAL FANTASY XI");
                        if (Directory.Exists(ffxiSubPath) && IsValidFfxiPath(ffxiSubPath))
                            return ffxiSubPath;
                            
                        ffxiSubPath = Path.Combine(installPath, "FINAL FANTASY XI");
                        if (Directory.Exists(ffxiSubPath) && IsValidFfxiPath(ffxiSubPath))
                            return ffxiSubPath;
                    }
                }
            }

            // Also check current user registry
            foreach (var regPath in registryPaths)
            {
                using var key = Registry.CurrentUser.OpenSubKey(regPath);
                if (key != null)
                {
                    var installPath = key.GetValue("InstallDir") as string 
                                   ?? key.GetValue("0001") as string
                                   ?? key.GetValue("Path") as string;
                    
                    if (!string.IsNullOrWhiteSpace(installPath))
                    {
                        if (Directory.Exists(installPath) && IsValidFfxiPath(installPath))
                            return installPath;
                        
                        // Try common FFXI subpaths
                        var ffxiSubPath = Path.Combine(installPath, "SquareEnix", "FINAL FANTASY XI");
                        if (Directory.Exists(ffxiSubPath) && IsValidFfxiPath(ffxiSubPath))
                            return ffxiSubPath;
                            
                        ffxiSubPath = Path.Combine(installPath, "FINAL FANTASY XI");
                        if (Directory.Exists(ffxiSubPath) && IsValidFfxiPath(ffxiSubPath))
                            return ffxiSubPath;
                    }
                }
            }
        }
        catch
        {
            // Registry access might fail, continue with other methods
        }

        return null;
    }

    public string GetValidationMessage(string path)
    {
        if (!Directory.Exists(path))
            return "❌ Directory does not exist";

        var missingFiles = new List<string>();
        
        if (!File.Exists(Path.Combine(path, "polboot.exe")))
            missingFiles.Add("polboot.exe");
        
        if (!File.Exists(Path.Combine(path, "ffximain.dll")))
            missingFiles.Add("ffximain.dll");

        if (missingFiles.Any())
        {
            return $"❌ Missing required files: {string.Join(", ", missingFiles)}";
        }

        // Additional validation - check for ROM folders which indicate proper FFXI install
        var romFolders = new[] { "ROM", "ROM2", "ROM3" };
        var hasRomFolder = romFolders.Any(folder => Directory.Exists(Path.Combine(path, folder)));
        
        if (hasRomFolder)
            return "✅ Valid FFXI installation (ROM folders found)";

        return "✅ Valid FFXI installation";
    }

    public string? AutoDetectPlayOnlineViewerPath()
    {
        // First try to find it relative to FFXI installation
        var ffxiPath = AutoDetectFfxiPath();
        if (ffxiPath != null)
        {
            // FFXI is typically:      PlayOnline\SquareEnix\FINAL FANTASY XI
            // POL Viewer is:          PlayOnline\SquareEnix\PlayOnlineViewer
            // So they're siblings in the SquareEnix folder
            
            var parentDir = Directory.GetParent(ffxiPath)?.FullName; // SquareEnix
            if (parentDir != null)
            {
                var polPath = Path.Combine(parentDir, "PlayOnlineViewer");
                if (IsValidPlayOnlineViewerPath(polPath))
                    return polPath;
            }
        }
        
        var candidatePaths = new List<string>
        {
            GetPolPathFromRegistry(),
            @"C:\Program Files (x86)\PlayOnline\SquareEnix\PlayOnlineViewer",
            @"C:\Program Files\PlayOnline\SquareEnix\PlayOnlineViewer",
            @"D:\Program Files (x86)\PlayOnline\SquareEnix\PlayOnlineViewer",
            @"D:\PlayOnline\SquareEnix\PlayOnlineViewer",
            @"C:\PlayOnline\SquareEnix\PlayOnlineViewer",
            // Steam paths
            @"D:\Games\SteamLibrary\steamapps\common\FFXINA\SquareEnix\PlayOnlineViewer",
            @"C:\Program Files (x86)\Steam\steamapps\common\FFXINA\SquareEnix\PlayOnlineViewer",
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "PlayOnline", "SquareEnix", "PlayOnlineViewer"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "PlayOnline", "SquareEnix", "PlayOnlineViewer"),
        };

        foreach (var path in candidatePaths.Where(p => !string.IsNullOrWhiteSpace(p)))
        {
            if (IsValidPlayOnlineViewerPath(path!))
                return path;
        }
        return null;
    }

    public bool IsValidPlayOnlineViewerPath(string path)
    {
        if (!Directory.Exists(path)) return false;
        return File.Exists(Path.Combine(path, "pol.exe"));
    }

    public string GetPolValidationMessage(string path)
    {
        if (!Directory.Exists(path)) return "❌ Directory does not exist";
        if (!File.Exists(Path.Combine(path, "pol.exe"))) return "❌ Missing required file: pol.exe";
        return "✅ Valid PlayOnline Viewer installation";
    }

    private string? GetPolPathFromRegistry()
    {
        try
        {
            var registryPaths = new[] {
                @"SOFTWARE\PlayOnline\InstallFolder",
                @"SOFTWARE\WOW6432Node\PlayOnline\InstallFolder"
            };
            foreach (var regPath in registryPaths)
            {
                using var key = Registry.LocalMachine.OpenSubKey(regPath);
                if (key != null)
                {
                    var installPath = key.GetValue("0001") as string ?? key.GetValue("InstallFolder") as string;
                    if (!string.IsNullOrWhiteSpace(installPath) && IsValidPlayOnlineViewerPath(installPath))
                        return installPath;
                }
            }
        }
        catch { }
        return null;
    }
}

using System.Diagnostics;

namespace MistXI.Launcher.Services;

public sealed class ProcessLauncher
{
    public void Start(string exePath, string args, string workDir, bool requireElevation = false)
    {
        var psi = new ProcessStartInfo
        {
            FileName = exePath,
            Arguments = args,
            WorkingDirectory = workDir,
            UseShellExecute = false, // Use false to execute directly like PowerShell
            CreateNoWindow = false   // Allow the process to show its window
        };
        
        // Request elevation if needed (for Ashita injection)
        if (requireElevation)
        {
            psi.UseShellExecute = true;
            psi.Verb = "runas";
        }
        
        Process.Start(psi);
    }

    public void OpenFolder(string path)
    {
        if (!Directory.Exists(path)) return;
        Process.Start(new ProcessStartInfo { FileName = path, UseShellExecute = true });
    }
}

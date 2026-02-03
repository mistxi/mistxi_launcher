using System;
using System.IO;
using System.IO.Compression;

namespace MistXI.PatchHelper;

/// <summary>
/// Elevated helper that performs privileged operations
/// Usage: 
///   MistXI.PatchHelper.exe dsp <zipPath> <ffxiPath>
///   MistXI.PatchHelper.exe copy <sourcePath> <destPath>
/// </summary>
class Program
{
    static int Main(string[] args)
    {
        if (args.Length < 2)
        {
            Console.Error.WriteLine("Usage:");
            Console.Error.WriteLine("  MistXI.PatchHelper.exe dsp <zipPath> <ffxiPath>");
            Console.Error.WriteLine("  MistXI.PatchHelper.exe copy <sourcePath> <destPath>");
            return 1;
        }

        var operation = args[0].ToLowerInvariant();

        try
        {
            return operation switch
            {
                "dsp" => InstallDspPatch(args),
                "copy" => CopyDataFolder(args),
                _ => InvalidOperation(operation)
            };
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ERROR: {ex.Message}");
            Console.Error.WriteLine(ex.ToString());
            return 99;
        }
    }

    static int InvalidOperation(string operation)
    {
        Console.Error.WriteLine($"ERROR: Unknown operation '{operation}'");
        Console.Error.WriteLine("Valid operations: dsp, copy");
        return 1;
    }

    static int InstallDspPatch(string[] args)
    {
        if (args.Length != 3)
        {
            Console.Error.WriteLine("Usage: MistXI.PatchHelper.exe dsp <zipPath> <ffxiPath>");
            return 1;
        }

        var zipPath = args[1];
        var ffxiPath = args[2];

        Console.WriteLine($"Installing DSP patch...");
        Console.WriteLine($"From: {zipPath}");
        Console.WriteLine($"To: {ffxiPath}");

        if (!File.Exists(zipPath))
        {
            Console.Error.WriteLine($"ERROR: Zip file not found: {zipPath}");
            return 2;
        }

        if (!Directory.Exists(ffxiPath))
        {
            Console.Error.WriteLine($"ERROR: FFXI directory not found: {ffxiPath}");
            return 3;
        }

        // Extract to temp directory
        var extractPath = Path.Combine(Path.GetTempPath(), "MistXI_DSP_Extract_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(extractPath);
        Console.WriteLine($"Extracting to: {extractPath}");

        ZipFile.ExtractToDirectory(zipPath, extractPath);
        Console.WriteLine("Extraction complete");

        // Copy all files to FFXI directory
        var files = Directory.GetFiles(extractPath, "*", SearchOption.AllDirectories);
        Console.WriteLine($"Copying {files.Length} files...");

        int copiedCount = 0;
        foreach (var file in files)
        {
            var relativePath = Path.GetRelativePath(extractPath, file);
            var destFile = Path.Combine(ffxiPath, relativePath);
            
            Directory.CreateDirectory(Path.GetDirectoryName(destFile)!);
            File.Copy(file, destFile, overwrite: true);
            copiedCount++;
            
            if (copiedCount % 10 == 0)
            {
                Console.WriteLine($"Copied {copiedCount}/{files.Length} files...");
            }
        }

        Console.WriteLine($"Successfully copied {copiedCount} files");

        // Create marker file
        var markerPath = Path.Combine(ffxiPath, ".mistxi_dsp_applied");
        File.WriteAllText(markerPath, DateTime.UtcNow.ToString("O"));
        Console.WriteLine("Created marker file");

        // Cleanup temp directory
        try
        {
            Directory.Delete(extractPath, true);
            Console.WriteLine("Cleaned up temp files");
        }
        catch
        {
            // Non-critical
        }

        Console.WriteLine("DSP patch installed successfully!");
        return 0;
    }

    static int CopyDataFolder(string[] args)
    {
        if (args.Length != 3)
        {
            Console.Error.WriteLine("Usage: MistXI.PatchHelper.exe copy <sourcePath> <destPath>");
            return 1;
        }

        var sourcePath = args[1];
        var destPath = args[2];

        Console.WriteLine($"Copying data folder...");
        Console.WriteLine($"From: {sourcePath}");
        Console.WriteLine($"To: {destPath}");

        if (!Directory.Exists(sourcePath))
        {
            Console.Error.WriteLine($"ERROR: Source directory not found: {sourcePath}");
            return 2;
        }

        // Backup existing data folder if it exists
        if (Directory.Exists(destPath))
        {
            var backupPath = destPath + ".backup_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");
            Console.WriteLine($"Backing up existing data folder to: {backupPath}");
            try
            {
                Directory.Move(destPath, backupPath);
                Console.WriteLine("Backup complete");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Could not backup: {ex.Message}");
                // Continue anyway
            }
        }

        // Get all files to copy
        var files = Directory.GetFiles(sourcePath, "*", SearchOption.AllDirectories);
        Console.WriteLine($"Found {files.Length} files to copy");

        if (files.Length == 0)
        {
            Console.Error.WriteLine("ERROR: Source directory is empty");
            return 4;
        }

        // Create destination directory
        Directory.CreateDirectory(destPath);
        Console.WriteLine($"Created destination directory");

        // Copy files
        int copiedCount = 0;
        foreach (var sourceFile in files)
        {
            var relativePath = Path.GetRelativePath(sourcePath, sourceFile);
            var destFile = Path.Combine(destPath, relativePath);
            
            // Create subdirectories as needed
            var destDir = Path.GetDirectoryName(destFile);
            if (destDir != null)
            {
                Directory.CreateDirectory(destDir);
            }

            // Copy file
            File.Copy(sourceFile, destFile, overwrite: true);
            copiedCount++;
            
            if (copiedCount % 50 == 0 || copiedCount == files.Length)
            {
                Console.WriteLine($"Copied {copiedCount}/{files.Length} files...");
            }
        }

        Console.WriteLine($"Successfully copied {copiedCount} files");
        Console.WriteLine("Data folder copy complete!");
        return 0;
    }
}

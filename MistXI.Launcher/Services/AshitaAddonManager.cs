namespace MistXI.Launcher.Services;

public sealed class AshitaAddonManager
{
    public class AddonInfo
    {
        public string Name { get; set; } = "";
        public string Path { get; set; } = "";
        public string Type { get; set; } = ""; // "addon" or "plugin"
        public string? Description { get; set; }
        public bool IsEnabled { get; set; }
    }

    public List<AddonInfo> ScanAddons(string ashitaDir)
    {
        var addons = new List<AddonInfo>();
        
        var addonsPath = Path.Combine(ashitaDir, "addons");
        if (Directory.Exists(addonsPath))
        {
            foreach (var dir in Directory.GetDirectories(addonsPath))
            {
                var addonName = Path.GetFileName(dir);
                var mainFile = Path.Combine(dir, $"{addonName}.lua");
                
                if (File.Exists(mainFile))
                {
                    addons.Add(new AddonInfo
                    {
                        Name = addonName,
                        Path = mainFile,
                        Type = "addon",
                        Description = TryGetDescription(mainFile)
                    });
                }
            }
        }
        
        return addons.OrderBy(a => a.Name).ToList();
    }

    public List<AddonInfo> ScanPlugins(string ashitaDir)
    {
        var plugins = new List<AddonInfo>();
        
        var pluginsPath = Path.Combine(ashitaDir, "plugins");
        if (Directory.Exists(pluginsPath))
        {
            foreach (var file in Directory.GetFiles(pluginsPath, "*.dll"))
            {
                var pluginName = Path.GetFileNameWithoutExtension(file);
                
                plugins.Add(new AddonInfo
                {
                    Name = pluginName,
                    Path = file,
                    Type = "plugin"
                });
            }
        }
        
        return plugins.OrderBy(p => p.Name).ToList();
    }

    private string? TryGetDescription(string luaFile)
    {
        try
        {
            var lines = File.ReadLines(luaFile).Take(20);
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (trimmed.StartsWith("--") && trimmed.Length > 3)
                {
                    var desc = trimmed.Substring(2).Trim();
                    if (desc.Length > 10 && !desc.StartsWith("[[") && !desc.StartsWith("Copyright"))
                        return desc;
                }
            }
        }
        catch
        {
            // Ignore errors reading description
        }
        return null;
    }

    public string GenerateAshitaScript(List<string> enabledAddons, List<string> enabledPlugins, int fpsCap = 0)
    {
        var sb = new StringBuilder();
        sb.AppendLine("##########################################################################");
        sb.AppendLine("# MistXI Launcher - Auto-generated Ashita script");
        sb.AppendLine($"# Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine("##########################################################################");
        sb.AppendLine();
        
        if (enabledPlugins.Any())
        {
            sb.AppendLine("# Load Plugins");
            foreach (var plugin in enabledPlugins)
            {
                sb.AppendLine($"/load {plugin}");
            }
            sb.AppendLine();
        }
        
        // CRITICAL: Wait for plugins/addons to initialize
        sb.AppendLine("##########################################################################");
        sb.AppendLine("# Wait for initialization");
        sb.AppendLine("#");
        sb.AppendLine("# Important: This wait is required! Without it, addons will not");
        sb.AppendLine("# load properly or see commands in this file!");
        sb.AppendLine("##########################################################################");
        sb.AppendLine("/wait 3");
        sb.AppendLine("##########################################################################");
        sb.AppendLine();
        
        if (enabledAddons.Any())
        {
            sb.AppendLine("# Load Addons");
            foreach (var addon in enabledAddons)
            {
                sb.AppendLine($"/addon load {addon}");
            }
            sb.AppendLine();
        }
        
        // Add FPS cap command if set (AFTER addons are loaded)
        if (fpsCap > 0)
        {
            sb.AppendLine("# FPS Cap");
            sb.AppendLine("/wait 3");
            sb.AppendLine($"/fps {fpsCap}");
        }
        
        return sb.ToString();
    }
}

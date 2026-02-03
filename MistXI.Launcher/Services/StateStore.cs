using System.Text.Json;
using MistXI.Launcher.Models;

namespace MistXI.Launcher.Services;

public sealed class StateStore
{
    private readonly string _path;

    public StateStore(string baseDir)
    {
        Directory.CreateDirectory(baseDir);
        _path = Path.Combine(baseDir, "state.json");
    }

    public LauncherState Load()
    {
        try
        {
            if (!File.Exists(_path)) return new LauncherState();
            var json = File.ReadAllText(_path);
            return JsonSerializer.Deserialize<LauncherState>(json) ?? new LauncherState();
        }
        catch
        {
            return new LauncherState();
        }
    }

    public void Save(LauncherState state)
    {
        var json = JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_path, json);
    }

    public string StatePath => _path;
}

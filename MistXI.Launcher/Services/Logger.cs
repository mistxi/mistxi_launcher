namespace MistXI.Launcher.Services;

public sealed class Logger
{
    private readonly object _lock = new();

    public string LogDir { get; }
    public string LogPath { get; }

    public Logger(string appName = "MistXILauncher")
    {
        var baseDir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        LogDir = Path.Combine(baseDir, appName, "logs");
        Directory.CreateDirectory(LogDir);
        LogPath = Path.Combine(LogDir, "launcher.log");

        Write("Logger initialized.");
    }

    public void Write(string message, Exception? ex = null)
    {
        var ts = DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm:ss.fff zzz");
        var line = $"[{ts}] {message}";
        if (ex != null)
        {
            line += Environment.NewLine + ex;
        }

        lock (_lock)
        {
            Directory.CreateDirectory(LogDir);
            File.AppendAllText(LogPath, line + Environment.NewLine);
        }
    }
}

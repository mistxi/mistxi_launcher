using MistXI.Launcher.Services;

namespace MistXI.Launcher;

public sealed class AppServices
{
    public required string BaseDir { get; init; }
    public required StateStore StateStore { get; init; }
    public required CredentialStore CredentialStore { get; init; }
    public required RebootGate RebootGate { get; init; }
    public required GitHubAshitaService Ashita { get; init; }
    public required GitHubXiLoaderService XiLoader { get; init; }
    public required IniService Ini { get; init; }
    public required ProcessLauncher Proc { get; init; }
    public required Logger Logger { get; init; }
    public required MistWebService MistWeb { get; init; }
    public required FfxiDetector FfxiDetector { get; init; }
    public required AshitaAddonManager AddonManager { get; init; }
    public required PlayOnlineService PlayOnline { get; init; }
    public required FfxiInstallerService FfxiInstaller { get; init; }

    public static AppServices Create()
    {
        var baseDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MistXILauncher");
        Directory.CreateDirectory(baseDir);
        
        var logger = new Logger();
        var http = new System.Net.Http.HttpClient();
        
        return new AppServices
        {
            BaseDir = baseDir,
            StateStore = new StateStore(baseDir),
            CredentialStore = new CredentialStore(),
            RebootGate = new RebootGate(),
            Ashita = new GitHubAshitaService(),
            XiLoader = new GitHubXiLoaderService(),
            Ini = new IniService(),
            Proc = new ProcessLauncher(),
            Logger = logger,
            MistWeb = new MistWebService(),
            FfxiDetector = new FfxiDetector(),
            AddonManager = new AshitaAddonManager(),
            PlayOnline = new PlayOnlineService(logger),
            FfxiInstaller = new FfxiInstallerService(http, logger)
        };
    }
}

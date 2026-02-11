namespace MistXI.Launcher.Models;

public sealed class LauncherState
{
    public string? FfxiDir { get; set; }
    public string? PlayOnlineViewerDir { get; set; }

    public bool SetupComplete { get; set; }
    public DateTimeOffset? PendingRebootAtUtc { get; set; }

    public string? SavedUser { get; set; }
    public string? SavedPassDpapiB64 { get; set; }

    // Patching workflow state
    public bool DspPatchInstalled { get; set; }
    public bool PlayOnlineUpdated { get; set; }
    public bool DataFolderCopied { get; set; }
    public bool AccountCreated { get; set; }

    public List<GameProfile> Profiles { get; set; } = new() { new GameProfile { Name = "Default" } };
    public string ActiveProfileName { get; set; } = "Default";
    
    // XiLoader version override (null = use latest)
    public string? XiLoaderVersion { get; set; } = null;
}

public sealed class GameProfile
{
    public string Name { get; set; } = "Default";
    public string? Username { get; set; }
    
    // ============================================================================
    // Display Settings
    // ============================================================================
    public int ResolutionWidth { get; set; } = 1920;
    public int ResolutionHeight { get; set; } = 1080;
    public bool Windowed { get; set; } = true;
    public int WindowMode { get; set; } = 3; // 0=Fullscreen, 1=Windowed, 3=Borderless
    
    // ============================================================================
    // Graphics Quality Settings
    // ============================================================================
    public int GraphicsQuality { get; set; } = 4; // 0-6 scale
    public bool HardwareMouseCursor { get; set; } = true;
    public bool MipMapping { get; set; } = true;
    public bool BumpMapping { get; set; } = false;
    public bool EnvDiffuseMapping { get; set; } = false;
    public int EnvironmentAnimations { get; set; } = 2; // 0=Off, 1=Normal, 2=Smooth
    public int TextureCompression { get; set; } = 2; // 0=High, 1=Low, 2=Uncompressed
    public int MapTextureCompression { get; set; } = 1; // 0=Compressed, 1=Uncompressed
    public int FontCompression { get; set; } = 2; // 0=Compressed, 1=Uncompressed, 2=High Quality
    public int GraphicsStabilization { get; set; } = 0; // 0=Off, 1=On
    
    // ============================================================================
    // Advanced Graphics (Direct3D8)
    // ============================================================================
    public int BackBufferCount { get; set; } = -1;
    public int MultiSampleType { get; set; } = -1;
    public int FpsCap { get; set; } = 0; // 0=Default/Uncapped, 1=60fps, 2=30fps
    public int PresentationInterval { get; set; } = -1;
    
    // ============================================================================
    // Visual Settings
    // ============================================================================
    public bool ShowOpeningMovie { get; set; } = false;
    public bool SimplifiedCharCreation { get; set; } = false;
    public int GammaBase { get; set; } = 0;
    public bool MaintainAspectRatio { get; set; } = false;
    
    // ============================================================================
    // Audio Settings
    // ============================================================================
    public bool SoundEnabled { get; set; } = true;
    public int SoundEffectsVolume { get; set; } = 20; // 12-20
    public bool SoundAlwaysOn { get; set; } = true; // Play when game in background
    
    // ============================================================================
    // Input Settings
    // ============================================================================
    // Keyboard
    public bool KeyboardBlockInput { get; set; } = false;
    public bool KeyboardBlockBindsDuringInput { get; set; } = true;
    public bool KeyboardSilentBinds { get; set; } = false;
    public bool KeyboardWindowsKeyEnabled { get; set; } = false;
    
    // Mouse
    public bool MouseBlockInput { get; set; } = false;
    public bool MouseUnhook { get; set; } = true;
    
    // Gamepad
    public bool GamepadAllowBackground { get; set; } = false;
    public bool GamepadDisableEnumeration { get; set; } = false;
    
    // ============================================================================
    // Map Settings
    // ============================================================================
    public int MapCompressionType { get; set; } = 2; // 0=Uncompressed, 1=Low, 2=High
    
    // ============================================================================
    // Developer/Debug Settings
    // ============================================================================
    public int LogLevel { get; set; } = 5; // 0=None, 1=Critical, 2=Error, 3=Warn, 4=Info, 5=Debug
    public bool CrashDumps { get; set; } = true;
    public bool AddonsSilent { get; set; } = true;
    public bool PluginsSilent { get; set; } = true;
    
    // ============================================================================
    // Addons & Plugins
    // ============================================================================
    public List<string> EnabledAddons { get; set; } = new();
    public List<string> EnabledPlugins { get; set; } = new();
}

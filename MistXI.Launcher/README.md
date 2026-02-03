# MistXI Launcher

A modern, feature-rich launcher for the MistXI FFXI private server.

## Features

✅ **Auto-Detection** - Automatically finds your FFXI installation  
✅ **First-Run Wizard** - Smooth onboarding experience for new users  
✅ **Profile System** - Save multiple game configurations with different graphics settings  
✅ **Addon Manager** - Enable/disable Ashita addons and plugins per profile  
✅ **Auto-Updates** - Automatically downloads and updates Ashita v4 and XiLoader  
✅ **Secure Credentials** - Optional encrypted storage of login credentials  
✅ **Cancellation Support** - Cancel long downloads with a single click  
✅ **Progress Tracking** - Visual progress bars for all operations  
✅ **Comprehensive Graphics Settings** - Full control over all Ashita v4 graphics options  

## Requirements

- Windows 10 or later (64-bit)
- .NET 8 Runtime (Desktop) - [Download here](https://dotnet.microsoft.com/download/dotnet/8.0/runtime)
- FINAL FANTASY XI installed

## Quick Start

### For Users

1. Download the latest `MistXI-Launcher-Setup-vX.X.X.exe` from releases
2. Run the installer
3. Launch MistXI Launcher from Start Menu or Desktop
4. Follow the first-run wizard
5. Click "START GAME" and enjoy!

### For Developers

#### Building from Source

**Prerequisites:**
- .NET 8 SDK - [Download](https://dotnet.microsoft.com/download/dotnet/8.0)
- Visual Studio 2022 or VS Code (optional, for IDE)

**Build Commands:**

```bash
# Restore dependencies
dotnet restore

# Build in Release mode
dotnet build -c Release

# Run the application
dotnet run

# Publish as single-file executable
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true

# Output will be in: bin/Release/net8.0-windows/win-x64/publish/
```

#### Creating an Installer

1. **Install Inno Setup**
   - Download from: https://jrsoftware.org/isdl.php
   - Install with default options

2. **Build the application**
   ```bash
   dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
   ```

3. **Update installer.iss**
   - Open `installer.iss` in Inno Setup Compiler
   - Verify the `Source:` path matches your publish output directory
   - Update version number if needed

4. **Compile the installer**
   - Click "Compile" in Inno Setup
   - Installer will be created in `installer/` directory

## Project Structure

```
MistXI.Launcher/
├── Assets/                 # Images and icons
├── Models/                 # Data models (LauncherState, GameProfile)
├── Services/              # Business logic services
│   ├── AshitaAddonManager.cs
│   ├── CredentialStore.cs
│   ├── FfxiDetector.cs
│   ├── GitHubAshitaService.cs
│   ├── GitHubXiLoaderService.cs
│   ├── IniService.cs
│   ├── Logger.cs
│   ├── MistWebService.cs
│   ├── ProcessLauncher.cs
│   ├── RebootGate.cs
│   └── StateStore.cs
├── Themes/                # WPF styling
│   └── MistTheme.xaml
├── Views/                 # UI pages
│   ├── AddonsView.xaml[.cs]
│   ├── HomeView.xaml[.cs]
│   ├── ProfilesView.xaml[.cs]
│   └── SettingsView.xaml[.cs]
├── App.xaml[.cs]
├── AppServices.cs
├── FirstRunWizard.xaml[.cs]
├── MainWindow.xaml[.cs]
├── icon.ico
└── installer.iss
```

## Features in Detail

### Profile System

Create multiple game profiles with different settings:
- **Display**: Resolution, windowed/fullscreen mode
- **Graphics**: Quality level (0-6), hardware mouse, mipmapping, bump mapping
- **Advanced**: Buffer counts, multi-sampling, refresh rates
- **Audio**: Sound effects volume
- **Addons**: Different addon/plugin combinations per profile

Profiles are saved in `%LOCALAPPDATA%\MistXILauncher\state.json`

### Addon Manager

- Automatically scans `runtime/ashita/addons/` and `runtime/ashita/plugins/`
- Enable/disable with checkboxes
- Generates `default.txt` script automatically
- Profile-specific addon configurations

### Graphics Settings

Full control over all FFXI registry settings per profile:
- Registry values 0000-0045 (windowed mode, resolution, etc.)
- Direct3D8 presentation parameters
- Back buffer configuration
- Multi-sampling and refresh rate control
- Map compression settings
- Sound volume

See [Ashita v4 Documentation](https://docs.ashitaxi.com/usage/configurations/#section-ffxiregistry) for details on each setting.

### First-Run Experience

Beautiful wizard that:
1. Auto-detects FFXI installation (or offers to download)
2. Validates installation files
3. Optionally configures PlayOnline Viewer path
4. Explains what will happen next
5. Creates initial profile

### Auto-Updates

Launcher automatically:
- Checks for latest Ashita v4 from GitHub
- Downloads and extracts updates
- Preserves user addons, plugins, and configurations
- Updates XiLoader for MistXI server connection

## Configuration Files

### State File
Location: `%LOCALAPPDATA%\MistXILauncher\state.json`

Contains:
- FFXI and PlayOnline Viewer paths
- All game profiles
- Active profile selection
- Saved credentials (encrypted)
- Setup completion status

### Log File
Location: `%LOCALAPPDATA%\MistXILauncher\logs\launcher.log`

Contains:
- All launcher operations
- Error messages and stack traces
- Timestamps for troubleshooting

### Generated Files

**Ashita Boot Config**
- Location: `runtime/ashita/config/boot/mistxi.ini`
- Generated per-profile on launch
- Contains all graphics settings, paths, and server info

**Ashita Script**
- Location: `runtime/ashita/scripts/default.txt`
- Generated from active profile's addon/plugin selections
- Loads addons and plugins automatically on game start

## Troubleshooting

### "FFXI not found" error

1. Click Settings → Auto-Detect
2. Or manually browse to your FFXI folder (typically `C:\Program Files (x86)\PlayOnline\SquareEnix\FINAL FANTASY XI`)
3. Must contain `polboot.exe` and `ffximain.dll`

### "Network error" during download

- Check your internet connection
- Try again (downloads resume automatically)
- Check if antivirus is blocking the launcher

### "Permission denied" errors

- Run launcher as administrator
- Check that antivirus isn't quarantining files
- Ensure you have write access to launcher folder

### Game won't launch

1. Check Settings → Open Launcher Data Folder → logs/launcher.log
2. Verify FFXI path is correct
3. Ensure Ashita downloaded successfully (check `runtime/ashita/Ashita-cli.exe` exists)
4. Try deleting `runtime/ashita` folder and let it re-download

### Addons don't load

- Ensure addons are enabled in Addons tab
- Check that `runtime/ashita/scripts/default.txt` was generated
- Verify addon files exist in `runtime/ashita/addons/`

## Development

### Code Style

- C# 11 features enabled
- Nullable reference types enabled
- Async/await for all I/O operations
- Proper error handling with specific exception types

### Architecture

**MVVM-lite Pattern:**
- Views contain UI and simple event handlers
- Services contain business logic
- Models contain data structures
- AppServices provides service injection

**State Management:**
- Single source of truth in LauncherState
- JSON serialization for persistence
- Automatic saving after changes

### Testing

Currently no unit tests (TODO). Recommended approach:
- Unit tests for services (StateStore, IniService, FfxiDetector)
- Integration tests for GitHubAshitaService
- UI tests for critical workflows

### Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Test thoroughly
5. Submit a pull request

## License

[Your License Here]

## Credits

- **Ashita v4** - https://github.com/AshitaXI/Ashita-v4beta
- **XiLoader** - For private server connectivity
- **MistXI Team** - For the amazing private server

## Support

- Discord: [Your Discord]
- Website: https://mistxi.com
- Issues: [GitHub Issues Link]

---

**Version:** 0.1.0  
**Last Updated:** January 2026

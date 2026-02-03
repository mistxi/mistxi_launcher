# MistXI Launcher

A modern, all-in-one launcher for the MistXI FFXI private server. Automates installation, patching, and configuration to get you playing in minutes instead of hours.

![MistXI Launcher](https://img.shields.io/badge/version-1.1.1-blue)
![.NET](https://img.shields.io/badge/.NET-8.0-purple)
![Platform](https://img.shields.io/badge/platform-Windows%2010%2F11-lightgrey)

## ✨ Features

### 🚀 Installation & Setup
- **One-Click FFXI Installation** - Downloads and installs FFXI automatically (~3.5 GB)
- **Automated Patching** - Handles DSP patch, PlayOnline updates, and data folder setup
- **Smart Auto-Detection** - Finds existing FFXI installations (Steam or retail)
- **First-Run Wizard** - Guided setup for new players

### 🎮 Game Management
- **Profile System** - Save unlimited configurations with different settings
- **Addon & Plugin Manager** - Visual interface for enabling/disabling addons
- **Auto-Updates** - Keeps Ashita v4 and XiLoader up-to-date automatically
- **FPS Control** - Safe frame rate limiting (30/60 FPS via Ashita's fps addon)

### ⚙️ Configuration
- **36+ Graphics Settings** - Complete control over display, quality, and performance
- **Window Modes** - Fullscreen, windowed, or borderless windowed
- **Audio & Input** - Volume, gamepad, keyboard, and mouse settings
- **Per-Profile Addons** - Different addon sets for each profile

### 🔒 Smart & Secure
- **UAC Elevation** - Runs as normal user, elevates only when needed
- **Credential Storage** - Optional encrypted username/password saving
- **Update Protection** - Your settings saved separately from Ashita defaults

## 📋 Requirements

- **OS:** Windows 10 or 11 (64-bit)
- **Framework:** .NET 8.0 Runtime (included in release)
- **Disk Space:** ~15 GB 
- **Internet:** Required for downloads and gameplay

## 🎯 Quick Start

### For Players

1. **Download the Launcher**
   - Get the latest release: [Download MistXI Launcher](https://github.com/mistxi/mistxi_launcher/releases/latest)
   - Download `MistXI.Launcher.exe` (portable - no installation needed)

2. **Run the Launcher**
   - Double-click the .exe file
   - First-run wizard will guide you through setup

3. **Install FFXI** (if needed)
   - Click "Download & Install FFXI" in the wizard
   - Or skip if you already have FFXI installed

4. **Complete Patching**
   - Follow the wizard's patching workflow
   - Launcher automates most steps (DSP patch, data copy)
   - PlayOnline file check requires manual interaction

5. **Create Account & Play**
   - Create account at [mistxi.com/create-account](https://mistxi.com/create-account)
   - Launch and enjoy!

**Total Time:** ~1 hour (mostly waiting for PlayOnline updates)

### For Developers/Contributors

#### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Visual Studio 2022 or VS Code (optional)

#### Building from Source

```bash
# Clone the repository
git clone https://github.com/mistxi/mistxi_launcher.git
cd mistxi_launcher

# Build in Release mode
cd MistXI.Launcher
dotnet build -c Release

# Or publish as single-file executable
dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true
```

Output will be in: `bin/Release/net8.0-windows/win-x64/publish/`

## 📂 Project Structure

```
MistXI.Launcher/
├── Assets/                 # Images and icons
├── Models/                 # Data models (LauncherState, GameProfile)
├── Services/              # Business logic
│   ├── AshitaAddonManager.cs      # Addon/plugin scanning and script generation
│   ├── CredentialStore.cs         # Encrypted credential storage
│   ├── FfxiDetector.cs           # Auto-detection of FFXI installations
│   ├── FfxiInstallerService.cs   # FFXI installer download/extraction
│   ├── GitHubAshitaService.cs    # Ashita updates from GitHub
│   ├── GitHubXiLoaderService.cs  # XiLoader updates from GitHub
│   ├── IniService.cs             # Ashita config generation
│   ├── Logger.cs                 # File logging
│   ├── PlayOnlineService.cs      # POL patching workflow
│   ├── ProcessLauncher.cs        # Game launch with elevation
│   └── StateStore.cs             # JSON state management
├── Views/                 # UI pages (WPF)
│   ├── AddonsView.xaml          # Addon/plugin manager
│   ├── HomeView.xaml            # Main launch page
│   ├── ProfilesView.xaml        # Profile configuration
│   └── SettingsView.xaml        # Advanced settings
├── Themes/
│   └── MistTheme.xaml            # Dark theme with teal accents
├── FirstRunWizard.xaml           # Setup wizard
├── MainWindow.xaml               # Main window
└── icon.ico

MistXI.PatchHelper/              # Elevated helper for privileged operations
└── Program.cs                    # Handles DSP patch & data copy
```

## 🔧 Features in Detail

### Profile System

Create unlimited profiles with different settings:
- **Display:** Resolution (default 2560x1440), window mode (fullscreen/windowed/borderless)
- **Graphics Quality:** Slider (0-6) + individual settings (mipmapping, bump mapping, etc.)
- **Advanced Graphics:** Back buffer, multi-sample, VSync control
- **Audio:** Volume (12-20 concurrent sounds), background audio
- **Input:** Keyboard, mouse, gamepad settings
- **Visual:** Gamma adjustment, aspect ratio, movie skip
- **Addons:** Per-profile addon/plugin selections

Profiles saved in: `%LocalAppData%\MistXILauncher\launcher-state.json`

### Addon Manager

**Pre-compiled with popular Addons:**
- config, distance, drawdistance, enternity, fps, hideconsole
- mapdot, minimapmon, timestamp, tparty, nomount

**Pre-compiled with popular Plugins:**
- minimap, screenshot, deeps

**Features:**
- Visual checkbox interface
- Auto-enables "addons" plugin when needed
- Generates `mistxi.txt` script automatically
- Scripts saved to: `%LocalAppData%\MistXILauncher\runtime\ashita\scripts\`

### FPS Control

Safe frame rate limiting using Ashita's fps addon:
- **30 FPS (Default)** - Natural 30 FPS, no commands
- **60 FPS (Recommended)** - Smooth gameplay via `/fps 1`
- **30 FPS (Forced)** - Explicit lock via `/fps 2`

No crash-prone refresh rate registry hacks!

### Smart Elevation

Launcher runs as normal user and only requests UAC when needed:
- **DSP Patch Installation** - Writes to `C:\Program Files (x86)\`
- **Data Folder Copy** - Copies PlayOnline data to FFXI

Uses embedded helper executable (MistXI.PatchHelper.exe) for privileged operations.

### FFXI Installer Automation

For users without FFXI installed:
1. Downloads official installer from Square Enix (5 parts, ~3.5 GB)
2. Extracts multi-part RAR automatically
3. Launches FFXISetup.exe
4. Prompts to clean up installer files after installation

### Auto-Updates

On every launch, the launcher:
- Checks for latest Ashita v4 from GitHub
- Checks for latest XiLoader from GitHub
- Downloads and updates if needed
- Preserves user configurations and addons

## 📁 File Locations

### User Data
- **State File:** `%LocalAppData%\MistXILauncher\launcher-state.json`
- **Logs:** `%LocalAppData%\MistXILauncher\logs\launcher.log`
- **Runtime:** `%LocalAppData%\MistXILauncher\runtime\`

### Generated Files
- **Ashita Config:** `runtime/ashita/config/boot/mistxi.ini`
- **Ashita Script:** `runtime/ashita/scripts/mistxi.txt`

## 🐛 Troubleshooting

### FFXI Not Detected
1. Click Settings → Browse
2. Navigate to your FFXI folder
3. Common locations:
   - `C:\Program Files (x86)\PlayOnline\SquareEnix\FINAL FANTASY XI`
   - `C:\Program Files (x86)\Steam\steamapps\common\FFXINA\SquareEnix\FINAL FANTASY XI`

### Addons Don't Load
- Ensure "addons" plugin is enabled (launcher does this automatically)
- Check `%LocalAppData%\MistXILauncher\runtime\ashita\scripts\mistxi.txt` exists
- Verify addons exist in `runtime/ashita/addons/`

### Permission Errors
- Approve UAC prompts when they appear
- Don't run entire launcher as admin (unnecessary)
- Check antivirus isn't blocking MistXI.PatchHelper.exe

### Game Won't Launch
1. Check logs: `%LocalAppData%\MistXILauncher\logs\launcher.log`
2. Verify FFXI path is correct
3. Ensure Ashita downloaded: Check for `runtime/ashita/Ashita-cli.exe`

### Version Mismatch Error
- Delete `\ROM\0\0.dat` from FFXI folder
- Run PlayOnline file check
- Or restart launcher to get latest updates

## 🏗️ Development

### Architecture

**Services Pattern:**
- Views handle UI and events
- Services contain business logic
- Models define data structures
- AppServices provides dependency injection

**State Management:**
- Single source of truth: `LauncherState`
- JSON serialization for persistence
- Auto-save after changes

**Async Everywhere:**
- All I/O operations use async/await
- Cancellation token support for downloads
- Progress reporting for long operations

### Code Quality

- C# 12 with .NET 8.0
- Nullable reference types enabled
- Proper exception handling
- Comprehensive logging

### Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## 📝 Changelog

### v1.1.1 (February 2026)
- ✨ Fullscreen mode support
- 🐛 Fixed profile dropdown switching

### v1.1.0 (February 2026)
- ✨ FFXI installer automation
- ✨ Smart privilege elevation (embedded helper)
- ✨ FPS addon integration
- ✨ Unified script generation
- ✨ Auto-cleanup installer files
- ✨ Enhanced POL auto-detection
- 🐛 Fixed path escaping in helper
- 🎨 Approved addon/plugin filtering

### v1.0.0 (January 2026)
- 🎉 Initial release
- Profile system with 36 settings
- Addon/plugin manager
- Auto-detection and first-run wizard
- Dark theme with teal accents

## 📜 License

This project is licensed under the MIT License - see the LICENSE file for details.

## 🙏 Credits

- **Ashita v4** - https://github.com/AshitaXI/Ashita-v4beta
- **XiLoader** - https://github.com/LandSandBoat/xiloader

## 💬 Support

- **Discord:** https://discord.gg/kQp9Vetk3d
- **Website:** https://mistxi.com
- **Issues:** https://github.com/mistxi/mistxi_launcher/issues

## Disclaimer
Final Fantasy XI and PlayOnline are registered trademarks of Square Enix Holdings Co., Ltd.
This launcher is an independent tool for connecting to the MistXI private server and does not contain
any Square Enix intellectual property. All game files, and credited third-party tools are downloaded 
from official sources during runtime and are not pre-packaged or re-distributed.

---

**Built with ❤️ for the FFXI private server community**

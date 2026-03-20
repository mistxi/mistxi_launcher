# MistXI Launcher

A modern, all-in-one launcher for the MistXI FFXI private server. Get playing in minutes with automated installation, patching, and configuration.

![MistXI Launcher](https://img.shields.io/badge/version-1.5.1-teal)
![.NET](https://img.shields.io/badge/.NET-8.0-purple)
![Platform](https://img.shields.io/badge/platform-Windows%2010%2F11-lightgrey)

## ✨ Key Features

- **One-Click FFXI Installation** - Automated download and setup (~3.5 GB)
- **Profile System** - Multiple configurations with encrypted credential storage
- **Addon Manager** - Visual interface with search and XIPivot DAT overlay support
- **Auto-Updates** - Keeps Ashita v4 and XiLoader current
- **Smart Patching** - Automated DSP patch and version management
- **Maintenance Detection** - Automatically detects server maintenance from API

## 🎯 Quick Start

1. **Download**: Get [MistXI.Launcher.exe](https://github.com/mistxi/mistxi_launcher/releases/latest)
2. **Run**: First-run wizard guides setup
3. **Install FFXI** (if needed): Launcher downloads and installs automatically
4. **Create Account**: [mistxi.com/create-account](https://mistxi.com/create-account)
5. **Play!**

**Total Setup Time:** ~1 hour (mostly automated)

## 📋 Requirements

- Windows 10/11 (64-bit)
- .NET 8.0 Runtime (included)
- ~15 GB disk space
- Internet connection

## ⚙️ What's Configurable

**Per-Profile Settings:**
- Display (resolution, window mode, borderless)
- Graphics quality (slider + 36 individual settings)
- Audio, input (keyboard/mouse/gamepad)
- Addons & plugins (enable/disable per profile)
- Encrypted credentials (optional)

**Global Features:**
- Server selection (75 Era / 99 Era / Dev Server)
- XIPivot DAT overlays for texture mods
- Advanced mode (manual INI control)
- FPS limiting (30/60 via Ashita addon)

## 🔧 Development

### Building from Source

```bash
git clone https://github.com/mistxi/mistxi_launcher.git
cd mistxi_launcher/MistXI.Launcher
dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true
```

Output: `bin/Release/net8.0-windows/win-x64/publish/`

### Project Structure

```
MistXI.Launcher/
├── Services/       # Business logic (installers, API, config)
├── Views/          # UI (Home, Profiles, Addons, Settings)
├── Models/         # Data models (LauncherState, GameProfile)
├── Themes/         # Dark theme with teal accents
└── MainWindow.xaml
```

## 🐛 Troubleshooting

**FFXI Not Detected?**  
Settings → Browse to your FFXI folder (typically `C:\Program Files (x86)\PlayOnline\SquareEnix\FINAL FANTASY XI`)

**Version Mismatch?**  
Settings → Fix Version Mismatch (POL-3331) - automatically applies DSP patch and triggers update

**Logs:**  
`%LocalAppData%\MistXILauncher\logs\launcher.log`

## 📝 Recent Changes

###v1.5.1 (March 2026)
- 🎨 Redesign of the home page to be more accommodating to smaller displays, moving the save user/pass function to the profile page
- 🎨 Streamlined addon list display spacing
- 🎨 Maintenance status now pulled from the API and will prevent game start during maintenance periods
- ✨ Added per-profile credential storage with encrypted password support
- ✨ Added a version status pane to the settings page, along with a visual indicator of if the version numbers are acceptable to play
- ✨ Full gamepad/controller support integrated into the Profile tab
- ✨ Advanced settings mode to disable INI generation for advanced users
- ✨ Revamped Addons page with search functionality and XIPivot quick-enable toggle
- ✨ Enhanced XIPivot management with Remove button and proper enable/disable functionality
- 🐛 Fixed a bug that prevented the active profile from being editable without clicking away and re-selecting
- 🐛 Fixed server dropdown selection not persisting between tab navigation
- 🐛 Fixed a bug that removed the DSP patch during a client update, requiring it to be re-applied

[Full Changelog](CHANGELOG.md)

## 💬 Support

- **Discord:** https://discord.gg/kQp9Vetk3d
- **Website:** https://mistxi.com
- **Issues:** https://github.com/mistxi/mistxi_launcher/issues

## 📜 License & Credits

MIT License - See LICENSE file

**Thanks to:**
- [Ashita v4](https://github.com/AshitaXI/Ashita-v4beta)
- [XiLoader](https://github.com/LandSandBoat/xiloader)
- [XIPivot](https://github.com/HealsCodes/XIPivot)

## Disclaimer

Final Fantasy XI and PlayOnline are registered trademarks of Square Enix Holdings Co., Ltd. This launcher is an independent tool for the MistXI private server. Game files are downloaded from official sources during runtime.

---

**Built with ❤️ for the FFXI private server community**

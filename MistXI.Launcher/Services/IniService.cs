using System.Text;
using MistXI.Launcher.Models;

namespace MistXI.Launcher.Services;

public sealed class IniService
{
    // Builds a mistxi.ini compatible with known-working Ashita v4 + XiLoader configs.
    public string BuildMistIni(string ffxiDir, string serverHost, string? user, string? pass, GameProfile? profile = null)
    {
        profile ??= new GameProfile(); // Use defaults if no profile provided
        
        var command = $"--server {serverHost}";
        if (!string.IsNullOrWhiteSpace(user) && !string.IsNullOrWhiteSpace(pass))
        {
            // Match the known-good format: --user <n> --pass <pass> (no quoting)
            command += $" --user {user} --pass {pass}";
        }
        
        // Use relative path from Ashita's working directory
        // XiLoader is in: ashita/bootloader/xiloader.exe
        // Ashita is launched from: ashita/
        // So relative path is: bootloader/xiloader.exe

        var ini = @";---------------------------------------------------------------------------------------------------
; Ashita v4 Boot Configurations
;
; This file holds the various important settings that are used to configure Ashita. This file is 
; loaded as soon as Ashita is injected into Final Fantasy XI. Please edit this file with caution!
;---------------------------------------------------------------------------------------------------
; Configuration Notes
;
;   Every configuration setting in this file is considered optional. This means that Ashita will,
;   internally, attempt to use default values if one is not given here, or if the one given is
;   invalid. (However, this does not mean using a blank file will result in a successful launch.)
;
;   Depending on your setup and if you're playing on retail or a private server, some settings will
;   be expected in this file to properly run the game.
;---------------------------------------------------------------------------------------------------
; Configuration Documentation & Help
;
;   For more information about the available configuration settings and their values, please visit
;   the Ashita Documentation website here: https://docs.ashitaxi.com/usage/configurations/
;
;   You can also join the Ashita Discord for support: https://discord.gg/Ashita
;---------------------------------------------------------------------------------------------------

[ashita.launcher]
autoclose   = 0
name        = MistXI

[ashita.boot]
; Private Server Usage
file        = .\bootloader\xiloader.exe
command     = {COMMAND}
gamemodule  = ffximain.dll
script      = mistxi.txt
args        = 

[ashita.fonts]
d3d8.disable_scaling = 0
d3d8.family = Arial
d3d8.height = 10

[ashita.input]
gamepad.allowbackground         = 0
gamepad.disableenumeration      = 0
keyboard.blockinput             = 0
keyboard.blockbindsduringinput  = 1
keyboard.silentbinds            = 0
keyboard.windowskeyenabled      = 0
mouse.blockinput                = 0
mouse.unhook                    = 1

[ashita.language]
playonline  = 2
ashita      = 2

[ashita.logging]
level       = 5
crashdumps  = 1

[ashita.misc]
addons.silent   = 0
aliases.silent  = 0
plugins.silent  = 0

[ashita.polplugins]
sandbox = 0

[ashita.polplugins.args]
;sandbox = 

[ashita.resources]
offsets.use_overrides   = 1
pointers.use_overrides  = 1
resources.use_overrides = 1

[ashita.taskpool]
threadcount = -1

[ashita.window.startpos]
x = -1
y = -1

[ffxi.direct3d8]
presentparams.backbufferformat                  = -1
presentparams.backbuffercount                   = {BACKBUFFER_COUNT}
presentparams.multisampletype                   = {MULTISAMPLE_TYPE}
presentparams.swapeffect                        = -1
presentparams.enableautodepthstencil            = -1
presentparams.autodepthstencilformat            = -1
presentparams.flags                             = -1
presentparams.fullscreen_refreshrateinhz        = 0
presentparams.fullscreen_presentationinterval   = {PRESENTATION_INTERVAL}
behaviorflags.fpu_preserve                      = 0

[ffxi.registry]
0000 = {WINDOWED}
0001 = {RES_WIDTH}
0002 = {RES_HEIGHT}
0003 = 4096
0004 = 4096
0005 = -1
0006 = -1
0007 = 1
0008 = -1
0009 = -1
0010 = -1
0011 = 2
0012 = -1
0013 = -1
0014 = -1
0015 = -1
0016 = -1
0017 = 0
0018 = {GRAPHICS_QUALITY}
0019 = {HARDWARE_MOUSE}
0020 = 0
0021 = {MIPMAPPING}
0022 = {BUMP_MAPPING}
0023 = {ENV_DIFFUSE}
0024 = -1
0025 = -1
0026 = -1
0027 = -1
0028 = 0
0029 = {SOUND_EFFECTS_VOLUME}
0030 = 0
0031 = 1002740646
0032 = 0
0033 = 0
0034 = {WINDOW_MODE}
0035 = 1
0036 = {MAP_COMPRESSION}
0037 = {RES_WIDTH}
0038 = {RES_HEIGHT}
0039 = 1
0040 = 0
0041 = 0
0042 = {FFXIDIR}
0043 = 1
0044 = 1
0045 = 0
padmode000 = -1
padsin000 = -1
padguid000 = -1
";

        ini = ini.Replace("{COMMAND}", command);
        ini = ini.Replace("{FFXIDIR}", ffxiDir);
        
        // Profile settings
        // WindowMode: 0=Fullscreen, 1=Windowed, 3=Borderless
        // Registry 0034: 0=Fullscreen, 1=Windowed, 3=Borderless Windowed
        ini = ini.Replace("{WINDOW_MODE}", profile.WindowMode.ToString());
        
        // Registry 0000 is for something else (keep old windowed logic for backward compat)
        string windowModeValue = profile.WindowMode switch
        {
            0 => "1",  // Fullscreen
            1 => "6",  // Windowed with borders
            3 => "3",  // Borderless windowed
            _ => "3"   // Default to borderless
        };
        ini = ini.Replace("{WINDOWED}", windowModeValue);
        ini = ini.Replace("{RES_WIDTH}", profile.ResolutionWidth.ToString());
        ini = ini.Replace("{RES_HEIGHT}", profile.ResolutionHeight.ToString());
        ini = ini.Replace("{GRAPHICS_QUALITY}", profile.GraphicsQuality.ToString());
        ini = ini.Replace("{HARDWARE_MOUSE}", profile.HardwareMouseCursor ? "1" : "0");
        ini = ini.Replace("{MIPMAPPING}", profile.MipMapping ? "1" : "0");
        ini = ini.Replace("{BUMP_MAPPING}", profile.BumpMapping ? "1" : "0");
        ini = ini.Replace("{ENV_DIFFUSE}", profile.EnvDiffuseMapping ? "1" : "0");
        ini = ini.Replace("{MAP_COMPRESSION}", profile.MapCompressionType.ToString());
        ini = ini.Replace("{SOUND_EFFECTS_VOLUME}", profile.SoundEffectsVolume.ToString());
        ini = ini.Replace("{BACKBUFFER_COUNT}", profile.BackBufferCount.ToString());
        ini = ini.Replace("{MULTISAMPLE_TYPE}", profile.MultiSampleType.ToString());
        ini = ini.Replace("{PRESENTATION_INTERVAL}", profile.PresentationInterval.ToString());

        // Normalize newlines to the current environment.
        return ini.Replace("\r\n", "\n").Replace("\n", Environment.NewLine);
    }
}

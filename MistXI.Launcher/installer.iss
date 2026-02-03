; MistXI Launcher - Inno Setup Installer Script
; This script creates a professional installer for the MistXI Launcher
;
; To build: 
; 1. Install Inno Setup from https://jrsoftware.org/isdl.php
; 2. Update the paths below to match your build output
; 3. Compile this script with Inno Setup Compiler

#define MyAppName "MistXI Launcher"
#define MyAppVersion "0.1.0"
#define MyAppPublisher "MistXI"
#define MyAppURL "https://mistxi.com"
#define MyAppExeName "MistXI.Launcher.exe"

[Setup]
; NOTE: The value of AppId uniquely identifies this application.
; Do not use the same AppId value in installers for other applications.
AppId={{8F9D2A3B-1E5C-4D7B-9A8E-2F3D4C5B6A7D}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
AllowNoIcons=yes
LicenseFile=LICENSE.txt
; Uncomment the following line if you have a license file
; LicenseFile=LICENSE.txt
OutputDir=installer
OutputBaseFilename=MistXI-Launcher-Setup-v{#MyAppVersion}
SetupIconFile=icon.ico
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=lowest
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64
; Minimum Windows 10
MinVersion=10.0
DisableProgramGroupPage=yes
UninstallDisplayIcon={app}\{#MyAppExeName}

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "quicklaunchicon"; Description: "{cm:CreateQuickLaunchIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked; OnlyBelowVersion: 6.1; Check: not IsAdminInstallMode

[Files]
; NOTE: Update this path to your actual publish output directory
Source: "bin\Release\net8.0-windows\win-x64\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
; NOTE: If you have a license file, uncomment this:
; Source: "LICENSE.txt"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon
Name: "{userappdata}\Microsoft\Internet Explorer\Quick Launch\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: quicklaunchicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[Code]
function InitializeSetup(): Boolean;
var
  ErrorCode: Integer;
  NetFrameworkInstalled: Boolean;
  ResultCode: Integer;
begin
  // Check if .NET 8 Runtime is installed
  NetFrameworkInstalled := RegKeyExists(HKLM, 'SOFTWARE\dotnet\Setup\InstalledVersions\x64\sharedhost') or
                          RegKeyExists(HKCU, 'SOFTWARE\dotnet\Setup\InstalledVersions\x64\sharedhost');
  
  if not NetFrameworkInstalled then
  begin
    if MsgBox('.NET 8 Runtime is required but not installed.' + #13#10 + #13#10 +
              'Would you like to download it now?', mbConfirmation, MB_YESNO) = IDYES then
    begin
      ShellExec('open', 'https://dotnet.microsoft.com/download/dotnet/8.0/runtime', '', '', SW_SHOW, ewNoWait, ErrorCode);
      Result := False;
      Exit;
    end
    else
    begin
      MsgBox('Installation cannot continue without .NET 8 Runtime.', mbError, MB_OK);
      Result := False;
      Exit;
    end;
  end;
  
  Result := True;
end;

[UninstallDelete]
Type: filesandordirs; Name: "{localappdata}\MistXILauncher"

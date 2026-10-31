; Inno Setup script. Build with installer\build-installer.ps1, which publishes the app first and passes the version.
#ifndef AppVersion
  #define AppVersion "1.0.0"
#endif
#ifndef PublishDir
  #define PublishDir "..\artifacts\publish"
#endif

#define AppName "Dotnetstore PersonalApp"
#define AppExe "Dotnetstore.PersonalApp.Host.exe"

[Setup]
; Never change AppId: it is how upgrades find the previous installation.
AppId={{7B0E2C1A-4D53-4F0B-9C6E-2A1F5D8B3E47}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher=Dotnetstore
DefaultDirName={autopf}\{#AppName}
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes
UninstallDisplayIcon={app}\{#AppExe}
SetupIconFile=..\src\Dotnetstore.PersonalApp.Host\Assets\avalonia-logo.ico
OutputDir=..\artifacts\installer
OutputBaseFilename=Dotnetstore.PersonalApp-Setup-{#AppVersion}
Compression=lzma2
SolidCompression=yes
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
; Per-machine by default; the user can choose a per-user install in the dialog.
PrivilegesRequired=admin
PrivilegesRequiredOverridesAllowed=dialog
WizardStyle=modern
CloseApplications=yes

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#AppName}"; Filename: "{app}\{#AppExe}"
Name: "{group}\Uninstall {#AppName}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExe}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#AppExe}"; Description: "{cm:LaunchProgram,{#AppName}}"; Flags: nowait postinstall skipifsilent

; The user's data (databases and settings in %LOCALAPPDATA%\Dotnetstore.PersonalApp) is deliberately left alone on uninstall.
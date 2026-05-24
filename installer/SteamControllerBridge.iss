#define MyAppName "Steam Controller Bridge"
#define MyAppVersion "0.7.1"
#define MyAppPublisher "Steam Controller Bridge contributors"
#define MyAppExeName "SteamControllerBridge.exe"

[Setup]
AppId={{A18A32F4-31F7-4F64-9BD4-9A7D5C7B65F2}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={localappdata}\Programs\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
LicenseFile=..\LICENSE
OutputDir=..\dist\installer
OutputBaseFilename=SteamControllerBridgeSetup-{#MyAppVersion}
Compression=lzma
SolidCompression=yes
WizardStyle=modern
SetupIconFile=..\SteamControllerBridge\app.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Additional shortcuts:"; Flags: unchecked
Name: "startup"; Description: "Start Steam Controller Bridge when Windows starts"; GroupDescription: "Startup:"

[Files]
Source: "..\dist\SteamControllerBridge-win-x64-self-contained\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon
Name: "{userstartup}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: startup

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Launch {#MyAppName}"; Flags: nowait postinstall skipifsilent

[Messages]
FinishedHeadingLabel=Setup has finished installing [name]

[Code]
function InitializeSetup(): Boolean;
begin
  MsgBox(
    'Steam Controller Bridge requires ViGEmBus to create the virtual Xbox 360 controller.' + #13#10#13#10 +
    'This installer does not install ViGEmBus automatically. If it is missing, the app will tell you when you turn the bridge on.',
    mbInformation,
    MB_OK);
  Result := True;
end;

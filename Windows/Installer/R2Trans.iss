#define AppName "R2Trans"
#define AppVersion GetEnv("R2TRANS_APP_VERSION")
#define Runtime GetEnv("R2TRANS_RUNTIME")
#define Publisher "R2Trans"
#define PublishDir GetEnv("R2TRANS_PUBLISH_DIR")

[Setup]
AppId={{F9748A9A-338B-4F63-BD82-312D5D880621}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#Publisher}
DefaultDirName={autopf}\R2Trans
DefaultGroupName=R2Trans
DisableProgramGroupPage=yes
OutputDir=..\dist
OutputBaseFilename=R2TransSetup-{#AppVersion}-{#Runtime}
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=admin
UninstallDisplayIcon={app}\R2Trans.exe

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\R2Trans"; Filename: "{app}\R2Trans.exe"
Name: "{group}\Uninstall R2Trans"; Filename: "{uninstallexe}"
Name: "{autodesktop}\R2Trans"; Filename: "{app}\R2Trans.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\R2Trans.exe"; Description: "Launch R2Trans"; Flags: nowait postinstall skipifsilent

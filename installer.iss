; Hardware Store Portal - Inno Setup

#ifndef APIEXE
  #define APIEXE "HardwareStorePortal.API.exe"
#endif

#define AppName "Hardware Store Portal"
#define AppVersion "1.1.0"

[Setup]
AppId={{B8C1E8C4-7F0C-4B55-9D13-8F7A4C6A3E21}
AppName={#AppName}
AppVersion={#AppVersion}
DefaultDirName={autopf}\Hardware Store Portal
DefaultGroupName=Hardware Store Portal
DisableProgramGroupPage=yes
OutputDir=setup\Output
OutputBaseFilename=HardwareStorePortal_Setup
Compression=lzma
SolidCompression=yes
WizardStyle=modern
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=admin
UninstallDisplayIcon={app}\{#APIEXE}
CloseApplications=yes
RestartApplications=no

[Dirs]
; The database, its backups and the login key live in ProgramData, outside {app}, so they survive
; upgrades and uninstalls. Every Windows user of this PC must be able to write there.
Name: "{commonappdata}\HardwareStorePortal"; Permissions: users-modify; Flags: uninsneveruninstall
Name: "{commonappdata}\HardwareStorePortal\Backups"; Permissions: users-modify; Flags: uninsneveruninstall

[Files]
Source: "setup\Build\Backend\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs; Excludes: "*.db,*.db-shm,*.db-wal"

[Icons]
Name: "{autodesktop}\Hardware Store Portal"; Filename: "powershell.exe"; Parameters: "-NoProfile -ExecutionPolicy Bypass -File ""{app}\LaunchHardwareStorePortal.ps1"" ""{#APIEXE}"""; WorkingDir: "{app}"; IconFilename: "{app}\{#APIEXE}"
Name: "{autoprograms}\Hardware Store Portal"; Filename: "powershell.exe"; Parameters: "-NoProfile -ExecutionPolicy Bypass -File ""{app}\LaunchHardwareStorePortal.ps1"" ""{#APIEXE}"""; WorkingDir: "{app}"; IconFilename: "{app}\{#APIEXE}"

[Run]
Filename: "powershell.exe"; Parameters: "-NoProfile -ExecutionPolicy Bypass -File ""{app}\LaunchHardwareStorePortal.ps1"" ""{#APIEXE}"""; WorkingDir: "{app}"; Description: "Launch Hardware Store Portal"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
Type: filesandordirs; Name: "{app}"

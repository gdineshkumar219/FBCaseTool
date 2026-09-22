; -------------------------------------------------------------------------------------
; FBCaseTool.template.iss ~ Inno Setup template for FBCaseTool (FogBugz Update / Fetch Case).
; VersionInjector fills in {Version} and writes FBCaseTool.iss; Ship.bat compiles it.
; -------------------------------------------------------------------------------------
[Setup]
AppId={{16214F14-E59D-4202-9945-EBB2F808F6BB}
AppName=FBCaseTool
AppVersion={Version}
AppPublisher=Trumpf Metamation
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64
DefaultDirName=C:\Metamation\FBCaseTool
DefaultGroupName=FBCaseTool
DisableProgramGroupPage=yes
LicenseFile=TMM_EULA_EN.rtf
Compression=lzma
SolidCompression=yes
OutputDir=..\..
OutputBaseFilename=Setup.FBCaseTool.{Version}
UninstallDisplayIcon={app}\FBCaseTool.exe
VersionInfoVersion={Version}
VersionInfoProductName=FBCaseTool
VersionInfoDescription=FogBugz Update / Fetch Case dialog

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"

[Files]
; The published app (FBCaseTool.exe, FBLib.dll, config.example.json, ...).
; config.json (holds the API token) is never packaged.
Source: "..\..\Publish\*"; DestDir: "{app}"; Excludes: "config.json,*.pdb"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\FBCaseTool"; Filename: "{app}\FBCaseTool.exe"; IconFilename: "{app}\FBCaseTool.exe"
Name: "{group}\Uninstall FBCaseTool"; Filename: "{uninstallexe}"
Name: "{commondesktop}\FBCaseTool"; Filename: "{app}\FBCaseTool.exe"; Tasks: desktopicon; IconFilename: "{app}\FBCaseTool.exe"

[Run]
Filename: "{app}\FBCaseTool.exe"; Description: "Launch FBCaseTool"; Flags: postinstall nowait skipifsilent

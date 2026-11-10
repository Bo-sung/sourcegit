; SourceGit Classic Preview - per-user Windows x64 installer script.
; Compiled by tools/Publish-ClassicPortable.ps1 (Inno Setup 6.7.3).
; Install scope: current user only (no admin). Data lives in {app}\data,
; created by this installer so the app runs in its portable-data mode and
; never touches the original SourceGit per-user AppData folder.

#ifndef StageDir
  #define StageDir "..\..\artifacts\local-distribution-20261110\staging"
#endif
#ifndef OutDir
  #define OutDir "..\..\artifacts\local-distribution-20261110\installer"
#endif
#ifndef PkgVersion
  #define PkgVersion "0-preview"
#endif

// SmokeMode: alternate-identity build used ONLY by the smoke test.
// Different AppId => its install/uninstall registry entries can never
// collide with or replace a production install. No shortcuts are created.
#ifdef SmokeMode
  #define AppGuid "{{822B4D61-2014-4A41-ACF5-4321584B8D0A}"
  #define ProductName "SourceGit Classic Preview (smoke fixture)"
#else
  #define AppGuid "{{56B78231-DB7B-43D8-9D48-4A8386E019B4}"
  #define ProductName "SourceGit Classic Preview"
#endif

; SmokeMode: [Icons] entries are omitted below; silent installs add no shortcuts.

[Setup]
AppId={#AppGuid}
AppName={#ProductName}
AppVersion={#PkgVersion}
AppPublisher=Bo-sung (personal fork of sourcegit-scm/sourcegit, MIT)
AppSupportURL=https://github.com/Bo-sung/sourcegit
DefaultDirName={localappdata}\Programs\SourceGitClassic
DisableProgramGroupPage=yes
DisableWelcomePage=no
LicenseFile={#StageDir}\LICENSE.txt
PrivilegesRequired=lowest
OutputDir={#OutDir}
OutputBaseFilename=SourceGitClassic-Preview-{#PkgVersion}-win-x64-setup
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
UninstallDisplayName={#ProductName}
SetupIconFile=..\..\src\App.ico
; unsigned preview build - intentionally not signed

[Languages]
Name: "ko"; MessagesFile: "compiler:Languages\Korean.isl"
Name: "en"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"

[Dirs]
; persistent => the directory (and everything the app writes into it)
; survives uninstall and in-place upgrade. This is the app's portable
; data dir: preference.json, avatars, process.lock etc.
Name: "{app}\data"

[Files]
Source: "{#StageDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
; NOTE: data\ is intentionally NOT in [Files]; it must stay untracked by the
; uninstaller so preferences / recent lists are preserved on uninstall+upgrade.

[Icons]
#ifndef SmokeMode
Name: "{autoprograms}\SourceGit Classic Preview"; Filename: "{app}\SourceGit.exe"
Name: "{autodesktop}\SourceGit Classic Preview"; Filename: "{app}\SourceGit.exe"; Tasks: desktopicon
#endif

[Run]
Filename: "{app}\SourceGit.exe"; Description: "{cm:LaunchProgram,SourceGit Classic Preview}"; Flags: nowait postinstall skipifsilent

[Code]
#ifdef SmokeMode
// The smoke fixture must never run next to (or mutate) a real production install.
function InitializeSetup: Boolean;
var
  UninstallKey: string;
begin
  UninstallKey := 'SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{56b78231-db7b-43d8-9d48-4a8386e019b4}_is1';
  if RegKeyExists(HKCU, UninstallKey) then
  begin
    MsgBox('A real SourceGit Classic production install is registered. The smoke fixture refuses to run.', mbCriticalError, MB_OK);
    Result := False;
    Exit;
  end;
  Result := True;
end;
#endif

// Seed data\preference.json only if absent (never overwrite an existing profile).
// Shape matches the app's System.Text.Json source-generated Preferences:
// top-level "Locale" ("ko_KR" per Models/Locales.cs) and Layout.UseClassicLayout.
procedure CurStepChanged(CurStep: TSetupStep);
var
  DataDir: string;
begin
  if CurStep = ssPostInstall then
  begin
    DataDir := AddBackslash(ExpandConstant('{app}')) + 'data';
    if not ForceDirectories(DataDir) then
      Log('SourceGitClassic: failed to create data dir');
    if (FileExists(AddBackslash(DataDir) + 'preference.json') = False) and
       FileExists(AddBackslash(ExpandConstant('{app}')) + 'seed-preference.json') then
      FileCopy(AddBackslash(ExpandConstant('{app}')) + 'seed-preference.json',
               AddBackslash(DataDir) + 'preference.json', False);
  end;
end;

// Leave {app}\data on disk after uninstall; tell the user where it is.
procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
begin
  if CurUninstallStep = usPostUninstall then
    Log('SourceGitClassic: ' + ExpandConstant('{app}\data') + ' was intentionally kept.');
end;

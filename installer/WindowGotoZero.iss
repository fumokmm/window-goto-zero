; Inno Setup 6 script — Window Goto Zero
; Build with: scripts\build-installer.ps1
; Output:     installer\output\WindowGotoZero-Setup-x.y.z.exe
;
; App is framework-dependent (small). Requires .NET 8 Desktop Runtime (x64).

#define MyAppName "Window Goto Zero"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "fumokmm"
#define MyAppURL "https://github.com/fumokmm/window-goto-zero"
#define MyAppExeName "WindowGotoZero.exe"
#define MyAppId "{{A7C3E9F2-4B1D-4E8A-9C2F-6D5E8A1B3C4D}"
#define DotNetRuntimeUrl "https://aka.ms/dotnet/8.0/windowsdesktop-runtime-win-x64.exe"

[Setup]
AppId={#MyAppId}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
DefaultDirName={localappdata}\Programs\WindowGotoZero
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
; Per-user install — no Administrator prompt for the app itself
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
OutputDir=output
OutputBaseFilename=WindowGotoZero-Setup-{#MyAppVersion}
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
UninstallDisplayIcon={app}\{#MyAppExeName}
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
VersionInfoVersion={#MyAppVersion}
VersionInfoProductName={#MyAppName}
SetupLogging=yes
SetupIconFile=..\Assets\app.ico
UninstallDisplayName={#MyAppName}

[Languages]
Name: "japanese"; MessagesFile: "compiler:Languages\Japanese.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
; Publish output from scripts\build.ps1 (framework-dependent)
Source: "..\dist\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs; Excludes: "*.pdb"

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[Code]
function IsDotNetDesktop8Installed: Boolean;
var
  BasePath: String;
  FindRec: TFindRec;
begin
  Result := False;
  BasePath := ExpandConstant('{commonpf64}\dotnet\shared\Microsoft.WindowsDesktop.App');
  if not DirExists(BasePath) then
    Exit;

  if FindFirst(BasePath + '\8.*', FindRec) then
  begin
    try
      repeat
        if (FindRec.Attributes and FILE_ATTRIBUTE_DIRECTORY) <> 0 then
        begin
          Result := True;
          Break;
        end;
      until not FindNext(FindRec);
    finally
      FindClose(FindRec);
    end;
  end;
end;

function InitializeSetup(): Boolean;
var
  ErrorCode: Integer;
begin
  Result := True;

  if IsDotNetDesktop8Installed then
    Exit;

  if MsgBox(
       '{#MyAppName} の実行には .NET 8 Desktop Runtime (x64) が必要です。' + #13#10 + #13#10 +
       'まだ入っていないようです。今すぐ公式インストーラーを開きますか？' + #13#10 +
       '（ランタイムのインストールには管理者権限が必要な場合があります）' + #13#10 + #13#10 +
       '「いいえ」を選んでもセットアップは続行できますが、起動時に失敗する可能性があります。',
       mbConfirmation, MB_YESNO) = IDYES then
  begin
    if not ShellExec('open', '{#DotNetRuntimeUrl}', '', '', SW_SHOWNORMAL, ewNoWait, ErrorCode) then
      MsgBox('ランタイムのダウンロードページを開けませんでした。' + #13#10 +
             '次の URL をブラウザで開いてください:' + #13#10 +
             '{#DotNetRuntimeUrl}', mbError, MB_OK);
  end;
end;

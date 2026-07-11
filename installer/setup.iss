#define MyAppName "YMBデスクトップカレンダー"
#define MyAppExeName "YmbDesktopCalendar.exe"
#define MyAppPublisher "yumebi"
#define MyAppURL "https://github.com/yumebi/ymb_desktop_calendar"

#ifndef MyAppVersion
  #define MyAppVersion "1.0.0"
#endif
#ifndef MyPublishDir
  #define MyPublishDir "..\src\KabeCale.App\bin\Release\net10.0-windows\win-x64\publish"
#endif
#ifndef MyOutputDir
  #define MyOutputDir "..\dist"
#endif

[Setup]
AppId={{644DC9A1-FB46-41A0-A74E-C2E7BB58261B}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}/releases
DefaultDirName={localappdata}\Programs\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
OutputDir={#MyOutputDir}
OutputBaseFilename=YmbDesktopCalendar-Setup-{#MyAppVersion}
SetupIconFile=..\src\KabeCale.App\Resources\app.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
ArchitecturesInstallIn64BitMode=x64compatible
ArchitecturesAllowed=x64compatible
CloseApplications=yes
RestartApplications=yes

[Languages]
Name: "japanese"; MessagesFile: "compiler:Languages\Japanese.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"

[Files]
Source: "{#MyPublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#MyAppName}}"; Flags: nowait postinstall skipifsilent

[Code]
{ フレームワーク依存配布のため、.NET 10 Desktop Runtime (x64)が入っていないと起動できない。
  共有ランタイムのフォルダ存在だけを見る簡易チェック(誤検知でブロックしないよう非致命)。 }
function IsDotNet10DesktopRuntimeInstalled(): Boolean;
var
  FindRec: TFindRec;
  BasePath: string;
begin
  Result := False;
  BasePath := ExpandConstant('{commonpf64}\dotnet\shared\Microsoft.WindowsDesktop.App');
  if FindFirst(BasePath + '\10.*', FindRec) then
  begin
    Result := True;
    FindClose(FindRec);
  end;
end;

function InitializeSetup(): Boolean;
begin
  Result := True;
  if not IsDotNet10DesktopRuntimeInstalled() then
  begin
    if MsgBox('このアプリの実行には .NET 10 Desktop Runtime (x64) が必要です。' + #13#10 +
       '未インストールの場合はインストール後に起動してください。' + #13#10#13#10 +
       'https://dotnet.microsoft.com/download/dotnet/10.0' + #13#10#13#10 +
       'このままセットアップを続けますか?', mbConfirmation, MB_YESNO) = IDNO then
      Result := False;
  end;
end;

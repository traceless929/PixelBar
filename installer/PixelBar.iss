; PixelBar Inno Setup 安装脚本
; CI: ISCC.exe installer\PixelBar.iss /DMyAppVersion=0.0.3 /DPublishDir=..\publish\app-install

#ifndef MyAppVersion
  #define MyAppVersion "0.0.3"
#endif

#ifndef PublishDir
  #define PublishDir "..\publish\app-install"
#endif

#define MyAppName "PixelBar"
#define MyAppPublisher "PixelBar Contributors"
#define MyAppURL "https://github.com/traceless929/PixelBar"
#define MyAppExeName "PixelBar.App.exe"

[Setup]
AppId={{A7C3E9F1-4B2D-4F8A-9E6C-1D5A0B3F7E2C}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}/issues
AppUpdatesURL={#MyAppURL}/releases
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
LicenseFile=
OutputDir=..\artifacts
OutputBaseFilename=PixelBar.App-v{#MyAppVersion}-setup
SetupIconFile=..\src\PixelBar.App\Assets\AppIcon.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0.17763
ChangesAssociations=no
ShowLanguageDialog=no

[Languages]
Name: "chinesesimplified"; MessagesFile: "compiler:Languages\ChineseSimplified.isl"

[Tasks]
Name: "desktopicon"; Description: "创建桌面快捷方式"; GroupDescription: "附加选项:"; Flags: unchecked

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\卸载 {#MyAppName}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "启动 {#MyAppName}"; Flags: nowait postinstall skipifsilent

[Code]
var
  GuidePage: TWizardPage;
  GuideMemo: TNewMemo;

function InitializeSetup(): Boolean;
var
  Version: TWindowsVersion;
begin
  Result := True;
  GetWindowsVersionEx(Version);
  if Version.Major < 10 then
  begin
    MsgBox('需要 Windows 10 或更高版本（64 位）。', mbError, MB_OK);
    Result := False;
    Exit;
  end;
  if not IsWin64 then
  begin
    MsgBox('PixelBar 仅支持 64 位 Windows。', mbError, MB_OK);
    Result := False;
  end;
end;

procedure InitializeWizard;
begin
  GuidePage := CreateCustomPage(
    wpSelectTasks,
    '使用前请了解',
    '安装完成后，请按以下步骤连接 PixelBar 设备：');

  GuideMemo := TNewMemo.Create(GuidePage);
  GuideMemo.Parent := GuidePage.Surface;
  GuideMemo.Left := ScaleX(0);
  GuideMemo.Top := ScaleY(0);
  GuideMemo.Width := GuidePage.SurfaceWidth;
  GuideMemo.Height := GuidePage.SurfaceHeight;
  GuideMemo.ReadOnly := True;
  GuideMemo.ScrollBars := ssVertical;
  GuideMemo.Text :=
    '1. 用 USB 连接 Halo PixelBar（VID/PID 0x2D99 / 0xA106）。' + #13#10 +
    '2. 若已安装 EDIFIER TempoHub，请先关闭 TempoHub，避免 HID 冲突。' + #13#10 +
    '3. 打开 PixelBar，在「设置」中刷新并选择设备，侧栏应显示「已连接」。' + #13#10 +
    '4. 使用 QQ 音乐动态歌词：播放歌曲后，在「动态歌词」页开启推送。' + #13#10 + #13#10 +
    '说明：本程序为第三方工具，固件升级等完整功能请使用官方 TempoHub / Connect。' + #13#10 +
    '文档：https://github.com/traceless929/PixelBar/wiki';
end;

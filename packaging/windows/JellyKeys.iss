; Build with Inno Setup 6. Pass /DPortableDir=<absolute portable folder> to ISCC.
#ifndef PortableDir
  #define PortableDir "..\..\builds\JellyKeys-1.0.0-win-x64\portable"
#endif

#define AppName "果冻键显 JellyKeys"
#define AppVersion "1.0.0"
#define AppExe "JellyKeys.exe"

[Setup]
AppId={{4B934329-443F-4D91-8593-43E2907E9AD5}
AppName={#AppName}
AppVersion={#AppVersion}
AppVerName={#AppName} {#AppVersion}
AppPublisher=米夏小雨
AppPublisherURL=https://mixiaxiaoyu.cc
AppSupportURL=https://mixiaxiaoyu.cc
DefaultDirName={localappdata}\Programs\JellyKeys
DefaultGroupName={#AppName}
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
CloseApplications=yes
RestartApplications=no
UninstallDisplayIcon={app}\{#AppExe}
SetupIconFile={#PortableDir}\JellyKeys.ico
OutputBaseFilename=JellyKeys-1.0.0-win-x64-setup
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
VersionInfoVersion=1.0.0.0
VersionInfoDescription=果冻键显 JellyKeys 安装程序
VersionInfoCompany=米夏小雨

[Languages]
Name: "chinesesimplified"; MessagesFile: "compiler:Languages\ChineseSimplified.isl"

[Tasks]
Name: "desktopicon"; Description: "创建桌面快捷方式"; GroupDescription: "附加选项："; Flags: unchecked

[Files]
Source: "{#PortableDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#AppName}"; Filename: "{app}\{#AppExe}"; WorkingDir: "{app}"; IconFilename: "{app}\JellyKeys.ico"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExe}"; WorkingDir: "{app}"; IconFilename: "{app}\JellyKeys.ico"; Tasks: desktopicon

[Run]
Filename: "{app}\{#AppExe}"; Description: "启动{#AppName}"; Flags: nowait postinstall skipifsilent

[Code]
procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  RunCommand: string;
  InstalledCommand: string;
begin
  if CurUninstallStep <> usUninstall then
    Exit;

  if RegQueryStringValue(HKCU, 'Software\Microsoft\Windows\CurrentVersion\Run',
    'JellyKeys', RunCommand) then
  begin
    InstalledCommand := '"' + ExpandConstant('{app}\JellyKeys.exe') + '"';
    if CompareText(RunCommand, InstalledCommand) = 0 then
      RegDeleteValue(HKCU, 'Software\Microsoft\Windows\CurrentVersion\Run',
        'JellyKeys');
  end;
end;

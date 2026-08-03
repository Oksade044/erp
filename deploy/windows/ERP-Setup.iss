; ERP Sistemi — Windows quraşdırıcısı (Inno Setup 6+)
; Self-contained Avalonia desktop (bütün .NET runtime + kitabxanalar daxil).
; Hədəf kompüterdə .NET quraşdırmaq lazım DEYİL.
;
; Qurma addımları (bax: deploy/windows/README.md):
;   1) dotnet publish src/Clients/ERP.Desktop/ERP.Desktop.csproj -c Release -r win-x64 \
;        --self-contained true -o <PublishDir>
;   2) <PublishDir>\server.url faylına API ünvanını yaz (məs. http://76.13.11.79)
;   3) ISCC.exe /DPublishDir=<PublishDir> deploy\windows\ERP-Setup.iss
;   4) Nəticə: Output\ERP-Setup.exe

#ifndef PublishDir
  #define PublishDir "..\..\publish\desktop"
#endif

#define MyAppName "ERP Sistemi"
#define MyAppVersion "1.0.0"
#define MyAppExe "ERP.Desktop.exe"

[Setup]
AppId={{A7F3C2E1-9B4D-4E6A-8C21-0A1B2C3D4E5F}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher=ERP
DefaultDirName={autopf}\ERP Sistemi
DefaultGroupName=ERP Sistemi
DisableProgramGroupPage=yes
UninstallDisplayIcon={app}\{#MyAppExe}
OutputDir=Output
OutputBaseFilename=ERP-Setup
SetupIconFile=..\..\src\Clients\ERP.Desktop\Assets\avalonia-logo.ico
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=admin

[Languages]
Name: "en"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Masaustunde qisayol yarat"; GroupDescription: "Elave qisayollar:"

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Excludes: "server.url"; Flags: recursesubdirs createallsubdirs ignoreversion
; server.url yalnız ilk quraşdırmada yazılır — istifadəçinin dəyişdiyi server ünvanı saxlanılır.
Source: "{#PublishDir}\server.url"; DestDir: "{app}"; Flags: onlyifdoesntexist

[Icons]
Name: "{group}\ERP Sistemi"; Filename: "{app}\{#MyAppExe}"
Name: "{group}\ERP Sistemi (Sil)"; Filename: "{uninstallexe}"
Name: "{autodesktop}\ERP Sistemi"; Filename: "{app}\{#MyAppExe}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExe}"; Description: "ERP Sistemini indi başlat"; Flags: nowait postinstall skipifsilent

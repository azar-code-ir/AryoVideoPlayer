; ═══════════════════════════════════════════════════════
; Aryo Video Player - Installer (Compressed)
; Inno Setup 6.3+ | https://jrsoftware.org/isdl.php
; ═══════════════════════════════════════════════════════

#define MyAppName "Aryo Video Player"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "AzarCoder Team"
#define MyAppURL "https://t.me/Thesurenax"
#define MyAppExeName "AryoVideoPlayer.exe"
#define MyAppAssocName "Aryo Video File"
#define MyAppAssocExt ".mp4"
#define MyAppAssocKey StringChange(MyAppAssocName, " ", "") + MyAppAssocExt

[Setup]
AppId={{A3F5B7C2-1D4E-4F6A-9B8C-2E5D7F1A3B4C}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
AllowNoIcons=yes
OutputDir=D:\VS CODE FILE\bottest\AryoVideoPlayer\installer
OutputBaseFilename=AryoVideoPlayer_Setup_{#MyAppVersion}
SetupIconFile=D:\VS CODE FILE\bottest\AryoVideoPlayer\Assets\app.ico
WizardStyle=modern
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
CloseApplications=yes
RestartApplications=no
DisableProgramGroupPage=yes
WizardImageFile=D:\VS CODE FILE\bottest\AryoVideoPlayer\Assets\logo-256x256.ico
WizardSmallImageFile=D:\VS CODE FILE\bottest\AryoVideoPlayer\Assets\logo-48x48.ico

[Languages]
Name: "persian"; MessagesFile: "compiler:Languages\French.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "associatefiles"; Description: "پیوست فایل‌های ویدیویی به {#MyAppName}"; GroupDescription: "فایل‌ها:"; Flags: checkedonce

[Files]
; ── فایل‌های اصلی برنامه ──
Source: "D:\VS CODE FILE\bottest\AryoVideoPlayer\bin\Release\net9.0-windows\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion
Source: "D:\VS CODE FILE\bottest\AryoVideoPlayer\bin\Release\net9.0-windows\AryoVideoPlayer.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "D:\VS CODE FILE\bottest\AryoVideoPlayer\bin\Release\net9.0-windows\AryoVideoPlayer.deps.json"; DestDir: "{app}"; Flags: ignoreversion
Source: "D:\VS CODE FILE\bottest\AryoVideoPlayer\bin\Release\net9.0-windows\AryoVideoPlayer.runtimeconfig.json"; DestDir: "{app}"; Flags: ignoreversion

; ── پکیج‌های NuGet ──
Source: "D:\VS CODE FILE\bottest\AryoVideoPlayer\bin\Release\net9.0-windows\LibVLCSharp.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "D:\VS CODE FILE\bottest\AryoVideoPlayer\bin\Release\net9.0-windows\LibVLCSharp.WPF.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "D:\VS CODE FILE\bottest\AryoVideoPlayer\bin\Release\net9.0-windows\Newtonsoft.Json.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "D:\VS CODE FILE\bottest\AryoVideoPlayer\bin\Release\net9.0-windows\Microsoft.Xaml.Behaviors.dll"; DestDir: "{app}"; Flags: ignoreversion

; ── کتابخانه‌های بومی VLC (فقط win-x64) ──
Source: "D:\VS CODE FILE\bottest\AryoVideoPlayer\bin\Release\net9.0-windows\libvlc\win-x64\*"; DestDir: "{app}\libvlc\win-x64"; Flags: recursesubdirs createallsubdirs ignoreversion

; ── FFmpeg ──
Source: "D:\VS CODE FILE\bottest\AryoVideoPlayer\bin\Release\net9.0-windows\ffmpeg.exe"; DestDir: "{app}"; Flags: ignoreversion

; ── Vosk (تشخیص گفتار) ──
Source: "D:\VS CODE FILE\bottest\AryoVideoPlayer\bin\Release\net9.0-windows\libvosk.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "D:\VS CODE FILE\bottest\AryoVideoPlayer\bin\Release\net9.0-windows\Vosk.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "D:\VS CODE FILE\bottest\AryoVideoPlayer\bin\Release\net9.0-windows\vosk_models\*"; DestDir: "{app}\vosk_models"; Flags: recursesubdirs createallsubdirs ignoreversion

; ── Runtime DLLs ──
Source: "D:\VS CODE FILE\bottest\AryoVideoPlayer\bin\Release\net9.0-windows\libgcc_s_seh-1.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "D:\VS CODE FILE\bottest\AryoVideoPlayer\bin\Release\net9.0-windows\libstdc++-6.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "D:\VS CODE FILE\bottest\AryoVideoPlayer\bin\Release\net9.0-windows\libwinpthread-1.dll"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\Uninstall {#MyAppName}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Registry]
Root: HKA; Subkey: "Software\Classes\{#MyAppAssocExt}\OpenWithProgids"; ValueType: string; ValueName: "{#MyAppAssocKey}"; ValueData: ""; Flags: uninsdeletevalue; Tasks: associatefiles
Root: HKA; Subkey: "Software\Classes\{#MyAppAssocKey}"; ValueType: string; ValueName: ""; ValueData: "{#MyAppAssocName}"; Flags: uninsdeletekey; Tasks: associatefiles
Root: HKA; Subkey: "Software\Classes\{#MyAppAssocKey}\DefaultIcon"; ValueType: string; ValueName: ""; ValueData: "{app}\{#MyAppExeName},0"; Tasks: associatefiles
Root: HKA; Subkey: "Software\Classes\{#MyAppAssocKey}\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#MyAppExeName}"" ""%1"""; Tasks: associatefiles
Root: HKA; Subkey: "Software\Classes\.avi\OpenWithProgids"; ValueType: string; ValueName: "{#MyAppAssocKey}"; ValueData: ""; Flags: uninsdeletevalue; Tasks: associatefiles
Root: HKA; Subkey: "Software\Classes\.mkv\OpenWithProgids"; ValueType: string; ValueName: "{#MyAppAssocKey}"; ValueData: ""; Flags: uninsdeletevalue; Tasks: associatefiles
Root: HKA; Subkey: "Software\Classes\.mov\OpenWithProgids"; ValueType: string; ValueName: "{#MyAppAssocKey}"; ValueData: ""; Flags: uninsdeletevalue; Tasks: associatefiles
Root: HKA; Subkey: "Software\Classes\.wmv\OpenWithProgids"; ValueType: string; ValueName: "{#MyAppAssocKey}"; ValueData: ""; Flags: uninsdeletevalue; Tasks: associatefiles
Root: HKA; Subkey: "Software\Classes\.flv\OpenWithProgids"; ValueType: string; ValueName: "{#MyAppAssocKey}"; ValueData: ""; Flags: uninsdeletevalue; Tasks: associatefiles
Root: HKA; Subkey: "Software\Classes\.webm\OpenWithProgids"; ValueType: string; ValueName: "{#MyAppAssocKey}"; ValueData: ""; Flags: uninsdeletevalue; Tasks: associatefiles

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

!include "MUI2.nsh"
!include "DotNetChecker.nsh"
!include "FileAssociation.nsh"
!addplugindir .\nsis-plugins

Name "SPCode"
OutFile "SPCode.Installer.exe"

InstallDir $APPDATA\spcode

RequestExecutionLevel admin

!define SHCNE_ASSOCCHANGED 0x8000000
!define SHCNF_IDLIST 0

!define MUI_ABORTWARNING
!define MUI_ICON "icon.ico"

!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_LICENSE "GPLv3.txt"
!insertmacro MUI_PAGE_COMPONENTS
!insertmacro MUI_PAGE_DIRECTORY
!insertmacro MUI_PAGE_INSTFILES
!insertmacro MUI_PAGE_FINISH

!insertmacro MUI_UNPAGE_WELCOME
!insertmacro MUI_UNPAGE_INSTFILES

!insertmacro MUI_LANGUAGE "English"

Section "Program" prog01
SectionIn 1 RO
SetOutPath $INSTDIR

!insertmacro CheckNetFramework 48

File SPCode.exe
File MahApps.Metro.dll
File ICSharpCode.AvalonEdit.dll
File System.Windows.Interactivity.dll
File Xceed.Wpf.AvalonDock.dll
File Xceed.Wpf.AvalonDock.Themes.Metro.dll
File smxdasm.dll
File QueryMaster.dll
File Ionic.BZip2.dll
File SourcepawnCondenser.dll
File ByteSize.dll
File Renci.SshNet.dll
File Newtonsoft.Json.dll
File DiscordRPC.dll
File ControlzEx.dll
File Octokit.dll
File Microsoft.WindowsAPICodePack.dll
File Microsoft.WindowsAPICodePack.Shell.dll

File lang_0_spcode.xml
File GPLv3.txt

CreateDirectory "$APPDATA\spcode\crashlogs"
CreateDirectory "$APPDATA\spcode\lysis"
CreateDirectory "$APPDATA\spcode\sourcepawn"
CreateDirectory "$APPDATA\spcode\sourcepawn\errorfiles"
CreateDirectory "$APPDATA\spcode\sourcepawn\temp"
CreateDirectory "$APPDATA\spcode\sourcepawn\templates"
CreateDirectory "$APPDATA\spcode\sourcepawn\configs"
CreateDirectory "$APPDATA\spcode\sourcepawn\configs\sm_1_10_0_6478"

SetOutPath $APPDATA\spcode
File /r ".\sourcepawn"
File /r ".\lysis"

IfFileExists $APPDATA\spcode\options_0.dat OptionsExist OptionsDoesNotExist
OptionsExist:
Delete $APPDATA\spcode\options_0.dat
OptionsDoesNotExist:

WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\spcode" "DisplayName" "SPCode - A lightweight SourcePawn editor"
WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\spcode" "UninstallString" "$INSTDIR\uninstall.exe"
WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\spcode" "InstallLocation" "$INSTDIR"
WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\spcode" "DisplayIcon" "$INSTDIR\SPCode.exe"
WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\spcode" "Publisher" "Hexah"
WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\spcode" "DisplayVersion" "1.6.x.x"
WriteRegDWORD HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\spcode" "NoModify" 1
WriteRegDWORD HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\spcode" "NoRepair" 1

WriteUninstaller $INSTDIR\uninstall.exe
SectionEnd


Section "File Association (.sp)" prog02
SectionIn 1
${registerExtension} "$INSTDIR\SPCode.exe" ".sp" "Sourcepawn Script"
System::Call 'Shell32::SHChangeNotify(i ${SHCNE_ASSOCCHANGED}, i ${SHCNF_IDLIST}, i 0, i 0)'
SectionEnd


Section "File Association (.inc)" prog03
SectionIn 1
${registerExtension} "$INSTDIR\SPCode.exe" ".inc" "Sourcepawn Include-File"
System::Call 'Shell32::SHChangeNotify(i ${SHCNE_ASSOCCHANGED}, i ${SHCNF_IDLIST}, i 0, i 0)'
SectionEnd


Section "File Association (.smx)" prog04
SectionIn 1
${registerExtension} "$INSTDIR\SPCode.exe" ".smx" "Sourcemod Plugin"
System::Call 'Shell32::SHChangeNotify(i ${SHCNE_ASSOCCHANGED}, i ${SHCNF_IDLIST}, i 0, i 0)'
SectionEnd


Section "Desktop Shortcut" prog05
SectionIn 1
CreateShortCut "$DESKTOP\SPCode.lnk" "$INSTDIR\SPCode.exe" ""
SectionEnd

Section "Startmenu Shortcut" prog06
SectionIn 1
CreateShortCut "$SMPROGRAMS\SPCode.lnk" "$INSTDIR\SPCode.exe" ""
SectionEnd

Section "Uninstall"

Delete $INSTDIR\uninstall.exe



Delete $INSTDIR\SPCode.exe
Delete $INSTDIR\MahApps.Metro.dll
Delete $INSTDIR\ICSharpCode.AvalonEdit.dll
Delete $INSTDIR\System.Windows.Interactivity.dll
Delete $INSTDIR\Xceed.Wpf.AvalonDock.dll
Delete $INSTDIR\Xceed.Wpf.AvalonDock.Themes.Metro.dll
Delete $INSTDIR\smxdasm.dll
Delete $INSTDIR\LysisForSpedit.dll
Delete $INSTDIR\QueryMaster.dll
Delete $INSTDIR\Ionic.BZip2.dll
Delete $INSTDIR\SourcepawnCondenser.dll
Delete $INSTDIR\Renci.SshNet.dll
Delete $INSTDIR\Newtonsoft.Json.dll
Delete $INSTDIR\DiscordRPC.dll
Delete $INSTDIR\Microsoft.WindowsAPICodePack.dll
Delete $INSTDIR\Microsoft.WindowsAPICodePack.Shell.dll

Delete $INSTDIR\lang_0_spcode.xml
Delete $INSTDIR\GPLv3.txt
Delete $INSTDIR\*.dat
RMDir /r $APPDATA\spcode
RMDir $INSTDIR

Delete "$DESKTOP\SPCode.lnk"
Delete "$SMPROGRAMS\SPCode.lnk"


DeleteRegKey HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\spcode"

${unregisterExtension} ".sp" "Sourcepawn Script"
${unregisterExtension} ".inc" "Sourcepawn Include-File"
${unregisterExtension} ".smx" "Sourcemod Plugin"
System::Call 'Shell32::SHChangeNotify(i ${SHCNE_ASSOCCHANGED}, i ${SHCNF_IDLIST}, i 0, i 0)'
 
SectionEnd
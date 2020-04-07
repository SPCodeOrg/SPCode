!include "MUI2.nsh"
!include "DotNetChecker.nsh"
!include "FileAssociation.nsh"

Name "SPEdit"
OutFile "SPEdit Installer.exe"

InstallDir $APPDATA\spedit

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

!insertmacro CheckNetFramework 45


File Spedit.exe
File MahApps.Metro.dll
File ICSharpCode.AvalonEdit.dll
File System.Windows.Interactivity.dll
File Xceed.Wpf.AvalonDock.dll
File Xceed.Wpf.AvalonDock.Themes.Metro.dll
File smxdasm.dll
File LysisForSpedit.dll
File QueryMaster.dll
File Ionic.BZip2.dll
File SourcepawnCondenser.dll
File Renci.SshNet.dll
File Newtonsoft.Json.dll
File DiscordRPC.dll

File lang_0_spedit.xml
File GPLv3.txt

IfFileExists $INSTDIR\options_0.dat OptionsExist OptionsDoesNotExist
OptionsExist:
Delete $INSTDIR\options_0.dat
OptionsDoesNotExist:

CreateDirectory "$INSTDIR\sourcepawn"
CreateDirectory "$INSTDIR\sourcepawn\errorfiles"
CreateDirectory "$INSTDIR\sourcepawn\scripts"
CreateDirectory "$INSTDIR\sourcepawn\temp"
CreateDirectory "$INSTDIR\sourcepawn\templates"
CreateDirectory "$INSTDIR\sourcepawn\configs"
CreateDirectory "$INSTDIR\sourcepawn\configs\sm_1_10_0_6478"
File /r ".\sourcepawn"

WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\spedit" "DisplayName" "SPEdit - A lightweight sourcepawn editor"
WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\spedit" "UninstallString" "$INSTDIR\uninstall.exe"
WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\spedit" "InstallLocation" "$INSTDIR"
WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\spedit" "DisplayIcon" "$INSTDIR\Spedit.exe"
WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\spedit" "Publisher" "Hexah"
WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\spedit" "DisplayVersion" "1.3.4.6"
WriteRegDWORD HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\spedit" "NoModify" 1
WriteRegDWORD HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\spedit" "NoRepair" 1

WriteUninstaller $INSTDIR\uninstall.exe
SectionEnd



Section "File Association (.sp)" prog02
SectionIn 1
${registerExtension} "$INSTDIR\Spedit.exe" ".sp" "Sourcepawn Script"
System::Call 'Shell32::SHChangeNotify(i ${SHCNE_ASSOCCHANGED}, i ${SHCNF_IDLIST}, i 0, i 0)'
SectionEnd


Section "File Association (.inc)" prog03
SectionIn 1
${registerExtension} "$INSTDIR\Spedit.exe" ".inc" "Sourcepawn Include-File"
System::Call 'Shell32::SHChangeNotify(i ${SHCNE_ASSOCCHANGED}, i ${SHCNF_IDLIST}, i 0, i 0)'
SectionEnd


Section "File Association (.smx)" prog04
SectionIn 1
${registerExtension} "$INSTDIR\Spedit.exe" ".smx" "Sourcemod Plugin"
System::Call 'Shell32::SHChangeNotify(i ${SHCNE_ASSOCCHANGED}, i ${SHCNF_IDLIST}, i 0, i 0)'
SectionEnd


Section "Desktop Shortcut" prog05
SectionIn 1
CreateShortCut "$DESKTOP\SPEdit.lnk" "$INSTDIR\Spedit.exe" ""
SectionEnd

Section "Startmenu Shortcut" prog06
SectionIn 1
CreateShortCut "$SMPROGRAMS\SPEdit.lnk" "$INSTDIR\Spedit.exe" ""
SectionEnd

Section "Uninstall"

Delete $INSTDIR\uninstall.exe



Delete $INSTDIR\Spedit.exe
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


Delete $INSTDIR\lang_0_spedit.xml
Delete $INSTDIR\GPLv3.txt
Delete $INSTDIR\*.dat
RMDir /r $INSTDIR\sourcepawn
RMDir $INSTDIR

Delete "$DESKTOP\SPEdit.lnk"
Delete "$SMPROGRAMS\SPEdit.lnk"


DeleteRegKey HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\spedit"

${unregisterExtension} ".sp" "Sourcepawn Script"
${unregisterExtension} ".inc" "Sourcepawn Include-File"
${unregisterExtension} ".smx" "Sourcemod Plugin"
System::Call 'Shell32::SHChangeNotify(i ${SHCNE_ASSOCCHANGED}, i ${SHCNF_IDLIST}, i 0, i 0)'
 
SectionEnd
md ".\sourcepawn"
md ".\sourcepawn\templates"
md ".\sourcepawn\configs"
md ".\sourcepawn\temp"
md ".\sourcepawn\errorfiles"
md ".\nsis-plugins"
md ".\lysis"
C:\Windows\system32\xcopy ".\..\..\Resources\Misc\Templates\*.*" ".\sourcepawn\templates\*.*" /e /y /q
C:\Windows\system32\xcopy ".\..\..\Resources\Misc\Configurations" ".\sourcepawn\configs" /e /y /q /d
C:\Windows\system32\xcopy ".\..\..\Resources\Misc\Lysis\*.*" ".\lysis" /e /y /q
copy ".\..\..\Resources\Translations\lang_0_spcode.xml" ".\lang_0_spcode.xml" /y
copy ".\..\..\Resources\License.txt" ".\License.txt" /y
copy ".\..\..\Deploy\DotNetChecker.nsh" ".\DotNetChecker.nsh" /y
copy ".\..\..\Deploy\FileAssociation.nsh" ".\FileAssociation.nsh" /y
copy ".\..\..\Deploy\GPLv3.txt" ".\GPLv3.txt" /y
copy ".\..\..\Deploy\nsis-plugins\DotNetChecker.dll" ".\nsis-plugins\DotNetChecker.dll" /y

set beta=0

if "%1"=="Debug-Beta" set beta=1
if "%1"=="Release-Beta" set beta=1

if %beta%==1 (
  copy ".\..\..\Deploy\SPCode_Beta.nsi" ".\SPCode.nsi" /y
  copy ".\..\..\Deploy\Compress_Beta.ps1" ".\Compress.ps1" /y

) else (
  copy ".\..\..\Deploy\SPCode_Stable.nsi" ".\SPCode.nsi" /y
  copy ".\..\..\Deploy\Compress_Stable.ps1" ".\Compress.ps1" /y
)

exit 0
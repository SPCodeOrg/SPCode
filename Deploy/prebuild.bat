REM // fetch git count for beta rev
for /f "tokens=*" %%A in ('git rev-list HEAD --count') do set "REV=%%A"

REM // fetch latest tag
for /f "tokens=*" %%B in ('git describe --match "[0-9].[0-9].[0-9].[0-9]" --tags --abbrev^=0') do set "TAG=%%B"

REM // make assemblyinfo from template
envsubst -i AssemblyInfo_Template.cs -o ..\App\AssemblyInfo.cs

REM // copy corresponding icons for version
set beta=0

if "%1"=="Debug-Beta" set beta=1
if "%1"=="Release-Beta" set beta=1

if %beta%==1 (
	copy "..\Resources\Icons\IconTemplates\Icon_Beta.ico" "..\Resources\Icons\Icon.ico" /y
	copy "..\Resources\Icons\IconTemplates\icon256xbeta.png" "..\Resources\Icons\icon256x.png" /y
	copy "..\Deploy\icon_beta.ico" ".\icon.ico" /y
) else (
	copy "..\Resources\Icons\IconTemplates\Icon.ico" "..\Resources\Icons\Icon.ico" /y
	copy "..\Resources\Icons\IconTemplates\icon256x.png" "..\Resources\Icons\icon256x.png" /y
	copy "..\Deploy\icon_stable.ico" ".\icon.ico" /y
)

exit 0
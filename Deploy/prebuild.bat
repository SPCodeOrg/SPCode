REM // set beta status
set beta=0
if "%1"=="Debug-Beta" set beta=1
if "%1"=="Release-Beta" set beta=1

REM // fetch git count for beta rev
for /f "tokens=*" %%A in ('git rev-list HEAD --count') do set "REV=%%A"

REM // fetch latest tag
for /f "tokens=*" %%B in ('git describe --match "[0-9].[0-9].[0-9].[0-9]" --tags --abbrev^=0') do set "TAG=%%B"

REM // set program name
set PROGRAMNAME=SPCode
if "%1"=="Debug-Beta" set PROGRAMNAME=SPCode Beta
if "%1"=="Release-Beta" set PROGRAMNAME=SPCode Beta

REM // make assemblyinfo and installer from template
envsubst -i AssemblyInfo_Template.cs -o ..\App\AssemblyInfo.cs

REM // copy corresponding icons for version
if %beta%==1 (
	copy "..\Resources\Icons\IconTemplates\Icon_Beta.ico" "..\Resources\Icons\Icon.ico" /y
	copy "..\Resources\Icons\IconTemplates\icon256xbeta.png" "..\Resources\Icons\icon256x.png" /y
) else (
	copy "..\Resources\Icons\IconTemplates\Icon.ico" "..\Resources\Icons\Icon.ico" /y
	copy "..\Resources\Icons\IconTemplates\icon256x.png" "..\Resources\Icons\icon256x.png" /y
)

exit 0
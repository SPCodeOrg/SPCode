echo --------------------------
echo **************************
echo BEGIN PREBUILD
echo **************************
echo --------------------------

REM // fetch git count for beta rev
for /f "tokens=*" %%A in ('git rev-list HEAD --count') do set "REV=%%A"

REM // fetch latest tag
for /f "tokens=*" %%B in ('git describe --match "[0-9].[0-9].[0-9].[0-9]" --tags --abbrev^=0') do set "TAG=%%B"

REM // make assemblyinfo from template
envsubst -i AssemblyInfo_Template.cs -o ..\App\AssemblyInfo.cs

exit 0
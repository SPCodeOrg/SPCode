REM // fetch git count for beta rev
for /f "tokens=*" %%A in ('git rev-list HEAD --count') do set "REV=%%A"

echo ****************************************
echo Directorio: %cd%
echo ****************************************

REM // fetch latest tag
for /f "tokens=*" %%B in ('git describe --tags --abbrev^=0') do set "TAG=%%B"

echo ****************************************
echo REVISION: %REV%
echo TAG: %TAG%
echo ****************************************

REM // make assemblyinfo from template
envsubst -i AssemblyInfo_Template.cs -o ..\App\AssemblyInfo.cs

exit 0



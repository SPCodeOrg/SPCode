REM // fetch git count for beta rev
for /f "tokens=*" %%A in ('git rev-list HEAD --count') do set "REV=%%A"

echo ****************************************
echo %cd%
echo ****************************************

REM // fetch latest tag
for /f "tokens=*" %%B in ('git describe --tags') do set "TAG=%%B"

REM // make assemblyinfo from template
envsubst -i AssemblyInfo_Template.cs -o ..\App\AssemblyInfo.cs

exit 0



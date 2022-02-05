$loc = Get-Location
Set-Location 'bin\Release\'
$compress = @{
LiteralPath= 
"sourcepawn/", 
"lysis/", 
"SPCode.exe", 
"GPLv3.txt",
"License.txt"
DestinationPath = "SPCode.Portable.zip"

}
Compress-Archive -Force @compress
Set-Location $loc
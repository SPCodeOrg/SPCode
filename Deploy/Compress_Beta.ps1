$loc = Get-Location
Set-Location 'bin\Release-Beta\'
$compress = @{
LiteralPath= 
"sourcepawn/", 
"lysis/", 
"SPCode.exe", 
"GPLv3.txt",
"License.txt"
DestinationPath = "SPCode.Beta.Portable.zip"

}
Compress-Archive -Force @compress
Set-Location $loc
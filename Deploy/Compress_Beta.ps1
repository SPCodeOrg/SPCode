$loc = Get-Location
Set-Location 'bin\Release-Beta\'
$compress = @{
LiteralPath= 
"sourcepawn/", 
"lysis/", 
"SPCode Beta.exe", 
"MdXaml.dll", 
"MahApps.Metro.dll", 
"ICSharpCode.AvalonEdit.dll", 
"ByteSize.dll", 
"System.Windows.Interactivity.dll", 
"Xceed.Wpf.AvalonDock.dll", 
"Xceed.Wpf.AvalonDock.Themes.Metro.dll", 
"smxdasm.dll", 
"ValveQuery.dll", 
"SourcepawnCondenser.dll", 
"Renci.SshNet.dll", 
"Newtonsoft.Json.dll", 
"DiscordRPC.dll", 
"ControlzEx.dll", 
"Octokit.dll", 
"Microsoft.WindowsAPICodePack.dll", 
"Microsoft.WindowsAPICodePack.Shell.dll", 
"lang_0_spcode.xml", 
"GPLv3.txt",
"License.txt"
DestinationPath = "SPCode.Beta.Portable.zip"

}
Compress-Archive -Force @compress
Set-Location $loc
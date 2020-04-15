$loc = Get-Location
Set-Location 'bin\Release\'
$compress = @{
LiteralPath= "sourcepawn/", "Spcode.exe", "MahApps.Metro.dll", "ICSharpCode.AvalonEdit.dll", "System.Windows.Interactivity.dll", "Xceed.Wpf.AvalonDock.dll", "Xceed.Wpf.AvalonDock.Themes.Metro.dll", "smxdasm.dll", "LysisForSpedit.dll", "QueryMaster.dll", "Ionic.BZip2.dll", "SourcepawnCondenser.dll", "Renci.SshNet.dll", "Newtonsoft.Json.dll", "DiscordRPC.dll", "ControlzEx.dll", "Octokit.dll", "lang_0_spcode.xml", "GPLv3.txt"
#Path= "sourcepawn/"
#CompressionLevel = "Fastest"
DestinationPath = "SPCode.Portable.zip"

}
Compress-Archive -Force @compress
Set-Location $loc
$loc = Get-Location
Set-Location 'bin\Release\'
$compress = @{
LiteralPath= "sourcepawn/", "Spedit.exe", "MahApps.Metro.dll", "ICSharpCode.AvalonEdit.dll", "System.Windows.Interactivity.dll", "Xceed.Wpf.AvalonDock.dll", "Xceed.Wpf.AvalonDock.Themes.Metro.dll", "smxdasm.dll", "LysisForSpedit.dll", "QueryMaster.dll", "Ionic.BZip2.dll", "SourcepawnCondenser.dll", "Renci.SshNet.dll", "Newtonsoft.Json.dll", "DiscordRPC.dll", "ControlzEx.dll", "lang_0_spedit.xml", "GPLv3.txt"
#Path= "sourcepawn/"
#CompressionLevel = "Fastest"
DestinationPath = "SPEdit.Portable.zip"

}
Compress-Archive -Force @compress
Set-Location $loc
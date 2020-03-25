using System;
using System.Text;
using System.Windows;
using System.Threading.Tasks;
using Spedit.Interop;
using QueryMaster;

namespace Spedit.UI
{
    public partial class MainWindow
    {
        private void Server_Query()
        {
            Config c = Program.Configs[Program.SelectedConfig];
            if (string.IsNullOrWhiteSpace(c.RConIP) || string.IsNullOrWhiteSpace(c.RConCommands))
            { return; }
            StringBuilder stringOutput = new StringBuilder();
            try
            {
                EngineType type = EngineType.GoldSource;
                if (c.RConUseSourceEngine)
                {
                    type = EngineType.Source;
                }
                using (Server server = ServerQuery.GetServerInstance(type, c.RConIP, c.RConPort, null))
                {
                    var serverInfo = server.GetInfo();
                    stringOutput.AppendLine(serverInfo.Name);
                    using (var rcon = server.GetControl(c.RConPassword))
                    {
                        string[] cmds = ReplaceRconCMDVariables(c.RConCommands).Split('\n');
                        foreach (var cmd in cmds)
                        {
                            Task t = Task.Run(() =>
                            {
                                string command = (cmd.Trim('\r')).Trim();
                                if (!string.IsNullOrWhiteSpace(command))
                                {
                                    stringOutput.AppendLine(rcon.SendCommand(command));
                                }
                            });
                            t.Wait();
                        }
                    }
                }
                stringOutput.AppendLine("Done");
            }
            catch (Exception e)
            {
                stringOutput.AppendLine("Error: " + e.Message);
            }
            CompileOutput.Text = stringOutput.ToString();
            if (CompileOutputRow.Height.Value < 11.0)
            {
                CompileOutputRow.Height = new GridLength(200.0);
            }
        }

        private string ReplaceRconCMDVariables(string input)
        {
            if (compiledFileNames.Count < 1)
            { return input; }
            if (input.IndexOf("{plugins_reload}", StringComparison.Ordinal) >= 0)
            {
                StringBuilder replacement = new StringBuilder();
                replacement.AppendLine();
                foreach (var fileName in compiledFileNames)
                {
                    replacement.Append("sm plugins reload " + StripSMXPostFix(fileName) + ";");
                }
                replacement.AppendLine();
                input = input.Replace("{plugins_reload}", replacement.ToString());
            }
            if (input.IndexOf("{plugins_load}", StringComparison.Ordinal) >= 0)
            {
                StringBuilder replacement = new StringBuilder();
                replacement.AppendLine();
                foreach (var fileName in compiledFileNames)
                {
                    replacement.Append("sm plugins load " + StripSMXPostFix(fileName) + ";");
                }
                replacement.AppendLine();
                input = input.Replace("{plugins_load}", replacement.ToString());
            }
            if (input.IndexOf("{plugins_unload}", StringComparison.Ordinal) >= 0)
            {
                StringBuilder replacement = new StringBuilder();
                replacement.AppendLine();
                foreach (var fileName in compiledFileNames)
                {
                    replacement.Append("sm plugins unload " + StripSMXPostFix(fileName) + ";");
                }
                replacement.AppendLine();
                input = input.Replace("{plugins_unload}", replacement.ToString());
            }
            return input;
        }

        private string StripSMXPostFix(string fileName)
        {
            if (fileName.EndsWith(".smx"))
            {
                return fileName.Substring(0, fileName.Length - 4);
            }
            return fileName;
        }
    }
}

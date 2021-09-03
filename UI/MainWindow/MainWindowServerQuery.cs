using System;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using QueryMaster;

namespace SPCode.UI
{
    public partial class MainWindow
    {
        /// <summary>
        /// Queries the server with the specified command in the command box of the config.
        /// </summary>
        private void Server_Query()
        {
            var c = Program.Configs[Program.SelectedConfig];
            if (string.IsNullOrWhiteSpace(c.RConIP) || string.IsNullOrWhiteSpace(c.RConCommands))
            {
                return;
            }

            var stringOutput = new StringBuilder();
            try
            {
                var type = EngineType.GoldSource;
                if (c.RConUseSourceEngine)
                {
                    type = EngineType.Source;
                }

                using (var server = ServerQuery.GetServerInstance(type, c.RConIP, c.RConPort, null))
                {
                    var serverInfo = server.GetInfo();
                    stringOutput.AppendLine(serverInfo.Name);
                    using var rcon = server.GetControl(c.RConPassword);
                    var cmds = ReplaceRconCMDVariables(c.RConCommands).Split('\n');
                    foreach (var cmd in cmds)
                    {
                        var t = Task.Run(() =>
                        {
                            var command = cmd.Trim('\r').Trim();
                            if (!string.IsNullOrWhiteSpace(command))
                            {
                                stringOutput.AppendLine(rcon.SendCommand(command));
                            }
                        });
                        t.Wait();
                    }
                }

                stringOutput.AppendLine("Done");
            }
            catch (Exception e)
            {
                stringOutput.AppendLine("Error: " + e.Message);
            }

            Dispatcher.Invoke(() =>
            {
                LogTextbox.Text = stringOutput.ToString();
                if (CompileOutputRow.Height.Value < 11.0)
                {
                    CompileOutputRow.Height = new GridLength(200.0);
                }
            });
        }

        /// <summary>
        /// Replaces the placeholders from the commands box with the corresponding content.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        private string ReplaceRconCMDVariables(string input)
        {
            if (CompiledFileNames.Count < 1)
            {
                return input;
            }

            if (input.IndexOf("{plugins_reload}", StringComparison.Ordinal) >= 0)
            {
                var replacement = new StringBuilder();
                replacement.AppendLine();
                foreach (var fileName in CompiledFileNames)
                {
                    replacement.Append("sm plugins reload " + StripSMXPostFix(fileName) + ";");
                }

                replacement.AppendLine();
                input = input.Replace("{plugins_reload}", replacement.ToString());
            }

            if (input.IndexOf("{plugins_load}", StringComparison.Ordinal) >= 0)
            {
                var replacement = new StringBuilder();
                replacement.AppendLine();
                foreach (var fileName in CompiledFileNames)
                {
                    replacement.Append("sm plugins load " + StripSMXPostFix(fileName) + ";");
                }

                replacement.AppendLine();
                input = input.Replace("{plugins_load}", replacement.ToString());
            }

            if (input.IndexOf("{plugins_unload}", StringComparison.Ordinal) >= 0)
            {
                var replacement = new StringBuilder();
                replacement.AppendLine();
                foreach (var fileName in CompiledFileNames)
                {
                    replacement.Append("sm plugins unload " + StripSMXPostFix(fileName) + ";");
                }

                replacement.AppendLine();
                input = input.Replace("{plugins_unload}", replacement.ToString());
            }

            return input;
        }

        /// <summary>
        /// Strips the '.smx' from the specified string
        /// </summary>
        /// <param name="fileName">File name to strip the extension from.</param>
        /// <returns></returns>
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